import { CustomerPicker } from './components/CustomerPicker'
import { OrderTracker } from './components/OrderTracker'
import { ProductGrid } from './components/ProductGrid'

function App() {

  return (
    <div className="min-h-screen bg-base-200">
      <nav className="navbar bg-base-100 shadow-sm px-4">
        <div className="flex-1">
          <span className="text-lg font-bold">Order Processing Demo</span>
        </div>
        <div className="flex-none gap-2 items-center">
          <span className="text-sm text-base-content/60">Customer:</span>
          <CustomerPicker />
        </div>
      </nav>

      <main className="container mx-auto px-4 py-8 grid grid-cols-1 lg:grid-cols-3 gap-8">
        <section className="lg:col-span-2 space-y-4">
          <h2 className="text-xl font-semibold">Products</h2>
          <ProductGrid />
        </section>

        <section className="space-y-4">
          <h2 className="text-xl font-semibold">Order Tracker</h2>
          <div className="card bg-base-100 shadow-sm">
            <div className="card-body">
              <OrderTracker />
            </div>
          </div>
        </section>
      </main>
    </div>
  )
}

export default App
