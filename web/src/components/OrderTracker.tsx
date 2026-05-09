import { useEffect } from 'react'
import { customerSelectors, customerThunks, useAppDispatch, useAppSelector } from '../store'
import type { OrderStatus, Order } from '../types'

const STATUS_COLOR: Record<OrderStatus, string> = {
  'Placed': 'badge-info',
  'Reserved':  'badge-warning',
  'Confirmed': 'badge-success',
  'Rejected':  'badge-error',
}

function OrderRow({ order }: { order: Order }) {
  return (
    <tr>
      <td className="font-mono text-xs">{order.id.slice(0, 8)}…</td>
      <td>{order.productName}</td>
      <td className="text-right">{order.quantity}</td>
      <td>
        <span className={`badge badge-sm ${STATUS_COLOR[order.status] ?? 'badge-ghost'}`}>
          {order.status}
        </span>
      </td>
    </tr>
  )
}

export function OrderTracker() {
  const customerId = useAppSelector(customerSelectors.selectCustomerId)
  const { orders, status, error } = useAppSelector(customerSelectors.selectCustomerState)
  const dispatch = useAppDispatch()

  useEffect(() => {
    dispatch(customerThunks.fetchOrders(customerId))
    const id = setInterval(() => dispatch(customerThunks.fetchOrders(customerId)), 2000)
    return () => clearInterval(id)
  }, [customerId, dispatch])

  if (status === 'loading' && orders.length === 0) {
    return <span className="loading loading-spinner loading-sm" />
  }

  if (status === 'error') {
    return <div className="alert alert-error text-sm">{error}</div>
  }

  if (orders.length === 0) {
    return (
      <p className="text-base-content/50 text-sm">
        Place an order to see it tracked here.
      </p>
    )
  }

  return (
    <div className="overflow-x-auto">
      <table className="table table-sm">
        <thead>
          <tr>
            <th>Order ID</th>
            <th>Product</th>
            <th className="text-right">Qty</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {orders.map(order => (
            <OrderRow key={order.id} order={order} />
          ))}
        </tbody>
      </table>
    </div>
  )
}
