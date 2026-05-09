export interface Product {
  id: string
  name: string
  sku: string
  price: number
  stockQuantity: number
}

export type OrderStatus = 'Placed' | 'Reserved' | 'Confirmed' | 'Rejected'

export interface Order {
  id: string
  customerId: string
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  status: OrderStatus
  createdAt: string
  updatedAt: string
}

export interface CreateOrderRequest {
  customerId: string
  productId: string
  productName: string
  quantity: number
  unitPrice: number
}
