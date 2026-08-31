import { getApiBaseUrl } from '../../../api/config'
import { ApiValidationError, authorizedFetch, throwIfNotOk } from '../../../api/client'
import type {
  CreateGameRequest,
  GetGameResponse,
  GetGamesQuery,
  GetGamesResponse,
  MutateGameResponse,
  UpdateGameRequest,
} from '../types/games'

export { ApiValidationError }

export const DEFAULT_PAGE_SIZE = 12
export const ADMIN_CATALOG_PAGE_SIZE = 20

export class GameNotFoundError extends Error {
  constructor(id: string) {
    super(`Game '${id}' was not found.`)
    this.name = 'GameNotFoundError'
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
    ? await authorizedFetch(path, { method: 'GET', signal }, 'You need to log in as an administrator.')
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
  const response = await authorizedFetch(
    '/api/games',
    {
      method: 'POST',
      signal,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    },
    'You need to log in as an administrator.',
  )
  await throwIfNotOk(response, 'Failed to create game', 'You do not have permission to manage the catalog.')
  return (await response.json()) as MutateGameResponse
}

export async function updateGame(
  id: string,
  request: UpdateGameRequest,
  signal?: AbortSignal,
): Promise<MutateGameResponse> {
  const response = await authorizedFetch(
    `/api/games/${id}`,
    {
      method: 'PUT',
      signal,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    },
    'You need to log in as an administrator.',
  )

  if (response.status === 404) {
    throw new GameNotFoundError(id)
  }

  await throwIfNotOk(response, 'Failed to update game', 'You do not have permission to manage the catalog.')
  return (await response.json()) as MutateGameResponse
}

export async function disableGame(id: string, signal?: AbortSignal): Promise<void> {
  const response = await authorizedFetch(
    `/api/games/${id}`,
    { method: 'DELETE', signal },
    'You need to log in as an administrator.',
  )

  if (response.status === 404) {
    throw new GameNotFoundError(id)
  }

  await throwIfNotOk(response, 'Failed to disable game', 'You do not have permission to manage the catalog.')
}
