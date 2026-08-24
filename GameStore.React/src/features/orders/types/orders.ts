export type OrderStatus = 'Pending' | 'Completed'

export type GetOrdersItem = {
  id: string
  status: OrderStatus
  totalAmount: number
  currency: string
  createdAt: string
}

export type GetOrdersResponse = {
  items: GetOrdersItem[]
}

export type GetOrderItemResponse = {
  gameId: string
  gameName: string
  price: number
}

export type GetOrderResponse = {
  id: string
  status: OrderStatus
  totalAmount: number
  currency: string
  createdAt: string
  items: GetOrderItemResponse[]
}
