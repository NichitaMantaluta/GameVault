import { getApiBaseUrl } from '../../../api/config'
import type { GetGamesQuery, GetGamesResponse } from '../types/games'

export const DEFAULT_PAGE_SIZE = 12

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
