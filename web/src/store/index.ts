import { useDispatch, useSelector } from 'react-redux'
import { customerSlice } from './customerSlice'
import { type AppDispatch, type RootState } from './store'

export const useAppDispatch = useDispatch.withTypes<AppDispatch>()
export const useAppSelector = useSelector.withTypes<RootState>()

export const customerActions = customerSlice.actions
export * as customerSelectors from './customerSelectors'
export * as customerThunks from './customerThunks'

export { store } from './store'
