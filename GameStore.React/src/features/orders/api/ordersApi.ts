import { authorizedFetch, throwIfNotOk } from '../../../api/client'
import type { GetOrderResponse, GetOrdersResponse } from '../types/orders'

export class OrderNotFoundError extends Error {
  constructor(id: string) {
    super(`Order '${id}' was not found.`)
    this.name = 'OrderNotFoundError'
  }
}

export async function getOrders(signal?: AbortSignal): Promise<GetOrdersResponse> {
  const response = await authorizedFetch(
    '/api/orders',
    { method: 'GET', signal },
    'You need to log in to view orders.',
  )
  await throwIfNotOk(response, 'Failed to load orders')
  return (await response.json()) as GetOrdersResponse
}

export async function getOrder(id: string, signal?: AbortSignal): Promise<GetOrderResponse> {
  const response = await authorizedFetch(
    `/api/orders/${id}`,
    { method: 'GET', signal },
    'You need to log in to view orders.',
  )

  if (response.status === 404) {
    throw new OrderNotFoundError(id)
  }

  await throwIfNotOk(response, 'Failed to load order')
  return (await response.json()) as GetOrderResponse
}
