import { createAsyncThunk } from '@reduxjs/toolkit';
import { api } from '../api';

export const fetchOrders = createAsyncThunk(
  'customer/fetchOrders',
  (customerId: string) => api.orders.listByCustomer(customerId),
)
