import { getApiBaseUrl } from '../../../api/config'
import type { GetGameResponse, GetGamesQuery, GetGamesResponse } from '../types/games'

export const DEFAULT_PAGE_SIZE = 12

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

  const queryString = params.toString()
  const url = `${getApiBaseUrl()}/api/games${queryString ? `?${queryString}` : ''}`
  const response = await fetch(url, { signal })

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
