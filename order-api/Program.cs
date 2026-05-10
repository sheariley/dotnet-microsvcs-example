using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using order_api.Data;
using order_api.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
  .AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(new InstrumentedProducerBuilder<string, string>(
    new ProducerConfig { BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] }));

builder.Services.AddSingleton<IProducer<string, string>>(sp =>
    sp.GetRequiredService<InstrumentedProducerBuilder<string, string>>().Build());

builder.Services.AddSingleton(new InstrumentedConsumerBuilder<string, string>(
    new ConsumerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
        GroupId = "order-api-group",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false,
    }));

builder.Services.AddHostedService<OrderEventConsumer>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(
        builder.Configuration["OTEL_SERVICE_NAME"] ?? "order-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddSource("OpenTelemetry.Instrumentation.ConfluentKafka")
        .AddKafkaProducerInstrumentation<string, string>()
        .AddKafkaConsumerInstrumentation<string, string>()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddKafkaProducerInstrumentation<string, string>()
        .AddKafkaConsumerInstrumentation<string, string>()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeScopes = true;
    o.IncludeFormattedMessage = true;
    o.AddOtlpExporter();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.MapControllers();
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

app.Run();
