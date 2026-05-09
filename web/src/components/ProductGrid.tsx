import { useEffect, useState } from 'react'
import { api } from '../api'
import { customerSelectors, useAppDispatch, useAppSelector } from '../store'
import { customerActions } from '../store'
import type { Product } from '../types'

export function ProductGrid() {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [placing, setPlacing] = useState<string | null>(null)
  const customerId = useAppSelector(customerSelectors.selectCustomerId)
  const dispatch = useAppDispatch()

  useEffect(() => {
    api.products.list()
      .then(setProducts)
      .catch(() => setError('Could not load products — is inventory running?'))
      .finally(() => setLoading(false))
  }, [])

  async function placeOrder(product: Product) {
    setPlacing(product.id)
    try {
      const order = await api.orders.create({
        customerId,
        productId: product.id,
        productName: product.name,
        quantity: 1,
        unitPrice: product.price,
      })
      dispatch(customerActions.addOrder(order))
    } catch {
      alert('Failed to place order — is order-api running?')
    } finally {
      setPlacing(null)
    }
  }

  if (loading) return <span className="loading loading-spinner loading-lg" />
  if (error) return <div className="alert alert-error">{error}</div>

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      {products.map(p => (
        <div key={p.id} className="card bg-base-100 border border-base-300 shadow-sm">
          <div className="card-body gap-1">
            <h3 className="card-title text-base">{p.name}</h3>
            <p className="text-xs text-base-content/60">{p.sku}</p>
            <p className="text-lg font-semibold">${p.price.toFixed(2)}</p>
            <p className="text-sm text-base-content/70">{p.stockQuantity} in stock</p>
            <div className="card-actions justify-end mt-2">
              <button
                className="btn btn-primary btn-sm"
                disabled={placing === p.id}
                onClick={() => placeOrder(p)}
              >
                {placing === p.id
                  ? <span className="loading loading-spinner loading-xs" />
                  : 'Order 1'}
              </button>
            </div>
          </div>
        </div>
      ))}
    </div>
  )
}
