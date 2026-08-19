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

export type UpdateCartItemRequest = {
  quantity: number
}
