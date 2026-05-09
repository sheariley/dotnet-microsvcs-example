import { useDispatch, useSelector } from 'react-redux'
import { customerSlice } from './customerSlice'
import { store, type AppDispatch, type RootState } from './store'
import { fetchOrders } from './customerThunks'

export const useAppDispatch = useDispatch.withTypes<AppDispatch>()
export const useAppSelector = useSelector.withTypes<RootState>()

export const customerActions = customerSlice.actions
export * as customerSelectors from './customerSelectors'
export * as customerThunks from './customerThunks'

export { store } from './store'

store.dispatch(fetchOrders(store.getState()[customerSlice.name].customerId))
