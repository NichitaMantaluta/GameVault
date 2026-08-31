import { authorizedFetch, throwIfNotOk } from '../../../api/client'
import type { AddCartItemRequest, CreateOrderResponse, GetCartResponse } from '../types/cart'

export const emptyCart: GetCartResponse = {
  items: [],
  subtotal: 0,
}

export async function getCart(signal?: AbortSignal): Promise<GetCartResponse> {
  const response = await authorizedFetch(
    '/api/cart',
    { method: 'GET', signal },
    'You need to log in to use the cart.',
  )
  await throwIfNotOk(response, 'Failed to load cart')
  return (await response.json()) as GetCartResponse
}

export async function addCartItem(
  request: AddCartItemRequest,
  signal?: AbortSignal,
): Promise<GetCartResponse> {
  const response = await authorizedFetch(
    '/api/cart/items',
    {
      method: 'POST',
      signal,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    },
    'You need to log in to use the cart.',
  )
  await throwIfNotOk(response, 'Failed to add item to cart')
  return (await response.json()) as GetCartResponse
}

export async function removeCartItem(gameId: string, signal?: AbortSignal): Promise<void> {
  const response = await authorizedFetch(
    `/api/cart/items/${gameId}`,
    { method: 'DELETE', signal },
    'You need to log in to use the cart.',
  )
  await throwIfNotOk(response, 'Failed to remove cart item')
}

export async function createOrder(signal?: AbortSignal): Promise<CreateOrderResponse> {
  const response = await authorizedFetch(
    '/api/orders',
    { method: 'POST', signal },
    'You need to log in to use the cart.',
  )
  await throwIfNotOk(response, 'Failed to start checkout')
  return (await response.json()) as CreateOrderResponse
}
