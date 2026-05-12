using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace order_api;

public static class ApiJsonOptions
{
    public static readonly JsonSerializerOptions Shared;

    static ApiJsonOptions()
    {
        Shared = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        Shared.MakeReadOnly();
    }
}
