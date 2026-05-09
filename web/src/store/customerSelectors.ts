import { createSelector } from '@reduxjs/toolkit'
import { type RootState } from './store'

export const selectCustomerState = createSelector(
  (state: RootState) => state.customer,
  state => state
)

export const selectCustomerId = createSelector(
  (state: RootState) => state.customer,
  (customerState) => customerState.customerId
)

export const selectOrders = createSelector(
  (state: RootState) => state.customer,
  (customerState) => customerState.orders
)
