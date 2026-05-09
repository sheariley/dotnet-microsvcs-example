import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import type { Order } from '../types'
import { fetchOrders } from './customerThunks'

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
