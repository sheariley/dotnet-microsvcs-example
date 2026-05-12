import { useEffect } from 'react'
import type { ReactNode } from 'react'
import { customerActions, customerSelectors, customerThunks, useAppDispatch, useAppSelector } from '../store'
import type { Order, OrderStatus } from '../types'

const WS_BASE = (import.meta.env.VITE_ORDER_API_URL ?? 'http://localhost:5001').replace(/^http/, 'ws')

type WsMessage =
  | { type: 'OrderCreated'; order: Order }
  | { type: 'OrderStatusChanged'; orderId: string; status: OrderStatus; updatedAt: string }

export function CustomerOrdersProvider({ children }: { children: ReactNode }) {
  const customerId = useAppSelector(customerSelectors.selectCustomerId)
  const dispatch = useAppDispatch()

  useEffect(() => {
    dispatch(customerThunks.fetchOrders(customerId))

    const ws = new WebSocket(`${WS_BASE}/ws/orders?customerId=${encodeURIComponent(customerId)}`)

    ws.onmessage = (event) => {
      const msg: WsMessage = JSON.parse(event.data as string)
      if (msg.type === 'OrderCreated') {
        dispatch(customerActions.addOrder(msg.order))
      } else if (msg.type === 'OrderStatusChanged') {
        dispatch(customerActions.updateOrder(msg))
      }
    }

    return () => ws.close()
  }, [customerId, dispatch])

  return <>{children}</>
}
