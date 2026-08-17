export type GetGamesQuery = {
  page?: number
  pageSize?: number
  search?: string
}

export type GetGamesItem = {
  id: string
  name: string
  description: string
  price: number
  imageUrl: string | null
  genreId: number
  genreName: string
}

export type GetGamesResponse = {
  items: GetGamesItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
