import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import type { Order, OrderStatus } from '../types'
import { fetchOrders } from './customerThunks'

interface OrderStatusUpdate {
  orderId: string
  status: OrderStatus
  updatedAt: string
}

export interface CustomerState {
  customerId: string
  orders: Order[]
  ordersFetchStatus: 'idle' | 'loading' | 'succeeded' | 'error'
  ordersFetchError: string | null
}

const initialState: CustomerState = {
  customerId: 'customer-alice',
  orders: [],
  ordersFetchStatus: 'idle',
  ordersFetchError: null,
}

export const customerSlice = createSlice({
  name: 'customer',
  initialState,
  reducers: {
    setCustomerId(state, action: PayloadAction<string>) {
      state.customerId = action.payload
    },
    addOrder(state, action: PayloadAction<Order>) {
      state.orders.unshift(action.payload)
    },
    updateOrder(state, action: PayloadAction<OrderStatusUpdate>) {
      const order = state.orders.find(o => o.id === action.payload.orderId)
      if (order) {
        order.status = action.payload.status
        order.updatedAt = action.payload.updatedAt
      }
    },
  },
  extraReducers(builder) {
    builder
      .addCase(fetchOrders.pending, state => {
        state.ordersFetchStatus = 'loading'
        state.ordersFetchError = null
      })
      .addCase(fetchOrders.fulfilled, (state, action) => {
        state.ordersFetchStatus = 'succeeded'
        state.orders = action.payload
      })
      .addCase(fetchOrders.rejected, (state, action) => {
        state.ordersFetchStatus = 'error'
        state.ordersFetchError = action.error.message ?? 'Failed to load orders'
      })
  },
})

export default customerSlice.reducer
