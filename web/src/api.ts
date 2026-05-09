import type { CreateOrderRequest, Order, Product } from './types'

const ORDER_API = import.meta.env.VITE_ORDER_API_URL ?? 'http://localhost:5001'
const INVENTORY_API = import.meta.env.VITE_INVENTORY_API_URL ?? 'http://localhost:5002'

async function json<T>(url: string, init?: RequestInit): Promise<T> {
  const res = await fetch(url, init)
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  return res.json() as Promise<T>
}

export const api = {
  products: {
    list: () => json<Product[]>(`${INVENTORY_API}/products`),
  },
  orders: {
    create: (req: CreateOrderRequest) =>
      json<Order>(`${ORDER_API}/orders`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(req),
      }),
    getById: (id: string) => json<Order>(`${ORDER_API}/orders/${id}`),
    listByCustomer: (customerId: string) =>
      json<Order[]>(`${ORDER_API}/orders?customerId=${encodeURIComponent(customerId)}`),
  },
}
