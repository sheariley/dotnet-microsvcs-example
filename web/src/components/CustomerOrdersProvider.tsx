import { useEffect } from 'react'
import type { ReactNode } from 'react'
import { customerActions, customerSelectors, customerThunks, useAppDispatch, useAppSelector } from '../store'
import type { OrderStatus } from '../types'

const WS_BASE = (import.meta.env.VITE_ORDER_API_URL ?? 'http://localhost:5001').replace(/^http/, 'ws')

interface OrderStatusMessage {
  orderId: string
  status: OrderStatus
  updatedAt: string
}

export function CustomerOrdersProvider({ children }: { children: ReactNode }) {
  const customerId = useAppSelector(customerSelectors.selectCustomerId)
  const dispatch = useAppDispatch()

  useEffect(() => {
    dispatch(customerThunks.fetchOrders(customerId))

    const ws = new WebSocket(`${WS_BASE}/ws/orders?customerId=${encodeURIComponent(customerId)}`)

    ws.onmessage = (event) => {
      const msg: OrderStatusMessage = JSON.parse(event.data as string)
      dispatch(customerActions.updateOrder(msg))
    }

    return () => ws.close()
  }, [customerId, dispatch])

  return <>{children}</>
}
