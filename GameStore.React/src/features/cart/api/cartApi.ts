import { getApiBaseUrl } from '../../../api/config'
import { getAccessToken } from '../../auth/keycloak'
import type { AddCartItemRequest, GetCartResponse, UpdateCartItemRequest } from '../types/cart'

export const emptyCart: GetCartResponse = {
  items: [],
  subtotal: 0,
}

export async function getCart(signal?: AbortSignal): Promise<GetCartResponse> {
  const response = await authorizedFetch('/api/cart', { method: 'GET', signal })
  await throwIfNotOk(response, 'Failed to load cart')
  return (await response.json()) as GetCartResponse
}

export async function addCartItem(
  request: AddCartItemRequest,
  signal?: AbortSignal,
): Promise<GetCartResponse> {
  const response = await authorizedFetch('/api/cart/items', {
    method: 'POST',
    signal,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  await throwIfNotOk(response, 'Failed to add item to cart')
  return (await response.json()) as GetCartResponse
}

export async function updateCartItem(
  gameId: string,
  request: UpdateCartItemRequest,
  signal?: AbortSignal,
): Promise<GetCartResponse> {
  const response = await authorizedFetch(`/api/cart/items/${gameId}`, {
    method: 'PATCH',
    signal,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  await throwIfNotOk(response, 'Failed to update cart item')
  return (await response.json()) as GetCartResponse
}

export async function removeCartItem(gameId: string, signal?: AbortSignal): Promise<void> {
  const response = await authorizedFetch(`/api/cart/items/${gameId}`, {
    method: 'DELETE',
    signal,
  })
  await throwIfNotOk(response, 'Failed to remove cart item')
}

async function authorizedFetch(path: string, init: RequestInit): Promise<Response> {
  const token = await getAccessToken()
  if (!token) {
    throw new Error('You need to log in to use the cart.')
  }

  const headers = new Headers(init.headers)
  headers.set('Authorization', `Bearer ${token}`)

  return fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
  })
}

async function throwIfNotOk(response: Response, fallback: string): Promise<void> {
  if (response.ok) {
    return
  }

  throw new Error(await readErrorMessage(response, fallback))
}

async function readErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const body = (await response.json()) as { detail?: unknown; title?: unknown }
    if (typeof body.detail === 'string' && body.detail.trim().length > 0) {
      return body.detail
    }
    if (typeof body.title === 'string' && body.title.trim().length > 0) {
      return body.title
    }
  } catch {
    // Use the fallback when the response is not problem+json.
  }

  return `${fallback} (${response.status}).`
}
