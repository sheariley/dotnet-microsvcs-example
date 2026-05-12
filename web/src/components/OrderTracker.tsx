import { customerSelectors, useAppSelector } from '../store'
import type { OrderStatus, Order } from '../types'

const fmtDateCell = (iso: string) =>
  new Date(iso).toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })

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
      <td className="text-xs text-base-content/60">{fmtDateCell(order.createdAt)}</td>
      <td className="text-xs text-base-content/60">{fmtDateCell(order.updatedAt)}</td>
    </tr>
  )
}

export function OrderTracker() {
  const { ordersFetchStatus, ordersFetchError } = useAppSelector(customerSelectors.selectOrdersFetchStatus)
  const orders = useAppSelector(customerSelectors.selectOrdersSortedByCreatedAt)

  if (ordersFetchStatus === 'loading' && orders.length === 0) {
    return <span className="loading loading-spinner loading-sm" />
  }

  if (ordersFetchStatus === 'error') {
    return <div className="alert alert-error text-sm">{ordersFetchError}</div>
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
            <th>Created</th>
            <th>Updated</th>
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
