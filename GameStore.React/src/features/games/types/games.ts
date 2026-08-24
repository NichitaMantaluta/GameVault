export type GetGamesQuery = {
  page?: number
  pageSize?: number
  search?: string
  includeInactive?: boolean
  statusFirst?: 'active' | 'inactive'
}

export type GetGamesItem = {
  id: string
  name: string
  description: string
  price: number
  imageUrl: string | null
  genreId: number
  genreName: string
  /** Present when the API supports it; omitted on older builds (active-only lists). */
  isActive?: boolean
}

export type GetGamesResponse = {
  items: GetGamesItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type GetGameResponse = {
  id: string
  name: string
  description: string
  price: number
  imageUrl: string | null
  genreId: number
  genreName: string
  createdAt: string
  updatedAt: string
  isActive: boolean
}

export type CreateGameRequest = {
  name: string
  description: string
  price: number
  genreId: number
  imageUrl?: string | null
}

export type UpdateGameRequest = {
  name: string
  description: string
  price: number
  genreId: number
  isActive: boolean
  imageUrl?: string | null
}

export type MutateGameResponse = {
  id: string
  name: string
  description: string
  price: number
  imageUrl: string | null
  genreId: number
  createdAt: string
  updatedAt: string
  isActive: boolean
}
