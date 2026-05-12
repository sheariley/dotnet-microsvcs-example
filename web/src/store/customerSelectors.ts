import { createSelector } from '@reduxjs/toolkit'
import { type RootState } from './store'

export const selectOrdersFetchStatus = createSelector(
  (state: RootState) => state.customer,
  state => ({ ordersFetchStatus: state.ordersFetchStatus, ordersFetchError: state.ordersFetchError })
)

export const selectCustomerId = createSelector(
  (state: RootState) => state.customer,
  (customerState) => customerState.customerId
)

export const selectOrders = createSelector(
  (state: RootState) => state.customer,
  (customerState) => customerState.orders
)

export const selectOrdersSortedByCreatedAt = createSelector(
  selectOrders,
  orders => [...orders].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
)
