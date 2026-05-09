import { customerActions, customerSelectors, useAppDispatch, useAppSelector } from '../store'

const CUSTOMERS = [
  { id: 'customer-alice', label: 'Alice' },
  { id: 'customer-bob', label: 'Bob' },
  { id: 'customer-carol', label: 'Carol' },
]

export function CustomerPicker() {
  const dispatch = useAppDispatch()
  const customerId = useAppSelector(customerSelectors.selectCustomerId)

  return (
    <select
      className="select select-bordered"
      value={customerId}
      onChange={e => dispatch(customerActions.setCustomerId(e.target.value))}
    >
      {CUSTOMERS.map(c => (
        <option key={c.id} value={c.id}>{c.label}</option>
      ))}
    </select>
  )
}
