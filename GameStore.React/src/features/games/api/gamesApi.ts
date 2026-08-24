import { getApiBaseUrl } from '../../../api/config'
import { getAccessToken } from '../../auth/keycloak'
import type {
  CreateGameRequest,
  GetGameResponse,
  GetGamesQuery,
  GetGamesResponse,
  MutateGameResponse,
  UpdateGameRequest,
} from '../types/games'

export const DEFAULT_PAGE_SIZE = 12
export const ADMIN_CATALOG_PAGE_SIZE = 20

export class GameNotFoundError extends Error {
  constructor(id: string) {
    super(`Game '${id}' was not found.`)
    this.name = 'GameNotFoundError'
  }
}

export class ApiValidationError extends Error {
  readonly fieldErrors: Record<string, string[]>

  constructor(message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiValidationError'
    this.fieldErrors = fieldErrors
  }
}

export async function getGames(
  query: GetGamesQuery = {},
  signal?: AbortSignal,
): Promise<GetGamesResponse> {
  const params = new URLSearchParams()

  if (query.page !== undefined) {
    params.set('page', String(query.page))
  }

  if (query.pageSize !== undefined) {
    params.set('pageSize', String(query.pageSize))
  }

  const search = query.search?.trim()
  if (search) {
    params.set('search', search)
  }

  if (query.includeInactive) {
    params.set('includeInactive', 'true')
  }

  if (query.statusFirst) {
    params.set('statusFirst', query.statusFirst)
  }

  const queryString = params.toString()
  const path = `/api/games${queryString ? `?${queryString}` : ''}`
  const response = query.includeInactive
    ? await authorizedFetch(path, { method: 'GET', signal })
    : await fetch(`${getApiBaseUrl()}${path}`, { signal })

  if (!response.ok) {
    throw new Error(`Failed to load games (${response.status}).`)
  }

  return (await response.json()) as GetGamesResponse
}

export async function getGame(id: string, signal?: AbortSignal): Promise<GetGameResponse> {
  const response = await fetch(`${getApiBaseUrl()}/api/games/${id}`, { signal })

  if (response.status === 404) {
    throw new GameNotFoundError(id)
  }

  if (!response.ok) {
    throw new Error(`Failed to load game (${response.status}).`)
  }

  return (await response.json()) as GetGameResponse
}

export async function createGame(
  request: CreateGameRequest,
  signal?: AbortSignal,
): Promise<MutateGameResponse> {
  const response = await authorizedFetch('/api/games', {
    method: 'POST',
    signal,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  await throwIfNotOk(response, 'Failed to create game')
  return (await response.json()) as MutateGameResponse
}

export async function updateGame(
  id: string,
  request: UpdateGameRequest,
  signal?: AbortSignal,
): Promise<MutateGameResponse> {
  const response = await authorizedFetch(`/api/games/${id}`, {
    method: 'PUT',
    signal,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (response.status === 404) {
    throw new GameNotFoundError(id)
  }

  await throwIfNotOk(response, 'Failed to update game')
  return (await response.json()) as MutateGameResponse
}

export async function disableGame(id: string, signal?: AbortSignal): Promise<void> {
  const response = await authorizedFetch(`/api/games/${id}`, {
    method: 'DELETE',
    signal,
  })

  if (response.status === 404) {
    throw new GameNotFoundError(id)
  }

  await throwIfNotOk(response, 'Failed to disable game')
}

async function authorizedFetch(path: string, init: RequestInit): Promise<Response> {
  const token = await getAccessToken()
  if (!token) {
    throw new Error('You need to log in as an administrator.')
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

  throw await readApiError(response, fallback)
}

async function readApiError(response: Response, fallback: string): Promise<Error> {
  try {
    const body = (await response.json()) as {
      detail?: unknown
      title?: unknown
      errors?: Record<string, string[] | undefined>
    }

    const fieldErrors: Record<string, string[]> = {}
    if (body.errors && typeof body.errors === 'object') {
      for (const [key, value] of Object.entries(body.errors)) {
        if (Array.isArray(value) && value.length > 0) {
          fieldErrors[key] = value.filter((entry): entry is string => typeof entry === 'string')
        }
      }
    }

    if (Object.keys(fieldErrors).length > 0) {
      const first = Object.values(fieldErrors)[0]?.[0]
      return new ApiValidationError(first ?? 'Please correct the highlighted fields.', fieldErrors)
    }

    if (typeof body.detail === 'string' && body.detail.trim().length > 0) {
      return new Error(body.detail)
    }

    if (typeof body.title === 'string' && body.title.trim().length > 0) {
      return new Error(body.title)
    }
  } catch {
    // Use the fallback when the response is not problem+json.
  }

  if (response.status === 403) {
    return new Error('You do not have permission to manage the catalog.')
  }

  return new Error(`${fallback} (${response.status}).`)
}
