import { getApiBaseUrl } from '../../../api/config'
import { getAccessToken } from '../../auth/keycloak'
import type { GetOrderResponse, GetOrdersResponse } from '../types/orders'

export class OrderNotFoundError extends Error {
  constructor(id: string) {
    super(`Order '${id}' was not found.`)
    this.name = 'OrderNotFoundError'
  }
}

export async function getOrders(signal?: AbortSignal): Promise<GetOrdersResponse> {
  const response = await authorizedFetch('/api/orders', { method: 'GET', signal })
  await throwIfNotOk(response, 'Failed to load orders')
  return (await response.json()) as GetOrdersResponse
}

export async function getOrder(id: string, signal?: AbortSignal): Promise<GetOrderResponse> {
  const response = await authorizedFetch(`/api/orders/${id}`, { method: 'GET', signal })

  if (response.status === 404) {
    throw new OrderNotFoundError(id)
  }

  await throwIfNotOk(response, 'Failed to load order')
  return (await response.json()) as GetOrderResponse
}

async function authorizedFetch(path: string, init: RequestInit): Promise<Response> {
  const token = await getAccessToken()
  if (!token) {
    throw new Error('You need to log in to view orders.')
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
