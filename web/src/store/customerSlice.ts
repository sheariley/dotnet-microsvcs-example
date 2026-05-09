import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import type { Order } from '../types'
import { fetchOrders } from './customerThunks'

export interface CustomerState {
  customerId: string
  orders: Order[]
  status: 'idle' | 'loading' | 'succeeded' | 'error'
  error: string | null
}

const initialState: CustomerState = {
  customerId: 'customer-alice',
  orders: [],
  status: 'idle',
  error: null,
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
  },
  extraReducers(builder) {
    builder
      .addCase(fetchOrders.pending, state => {
        state.status = 'loading'
        state.error = null
      })
      .addCase(fetchOrders.fulfilled, (state, action) => {
        state.status = 'succeeded'
        state.orders = action.payload
      })
      .addCase(fetchOrders.rejected, (state, action) => {
        state.status = 'error'
        state.error = action.error.message ?? 'Failed to load orders'
      })
  },
})

export default customerSlice.reducer
