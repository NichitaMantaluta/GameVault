export type GetCartItemResponse = {
  gameId: string
  name: string
  price: number
  imageUrl: string | null
  quantity: number
  lineTotal: number
}

export type GetCartResponse = {
  items: GetCartItemResponse[]
  subtotal: number
}

export type AddCartItemRequest = {
  gameId: string
}

export type CreateOrderItemResponse = {
  gameId: string
  gameName: string
  price: number
}

export type CreateOrderResponse = {
  id: string
  status: string
  totalAmount: number
  currency: string
  createdAt: string
  checkoutUrl: string
  items: CreateOrderItemResponse[]
}
