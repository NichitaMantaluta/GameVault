/** Matches seeded genres in GameStore.Api Persistence/seed-dev-data.sql. */
export const CATALOG_GENRES: Array<{ id: number; name: string }> = [
  { id: 1, name: 'Action' },
  { id: 2, name: 'Adventure' },
  { id: 3, name: 'RPG' },
  { id: 4, name: 'Strategy' },
  { id: 5, name: 'Simulation' },
  { id: 6, name: 'Sports' },
  { id: 7, name: 'Racing' },
  { id: 8, name: 'Puzzle' },
  { id: 9, name: 'Horror' },
  { id: 10, name: 'Platformer' },
  { id: 11, name: 'Fighting' },
  { id: 12, name: 'Shooter' },
]

export function genreOptionsForSelect(current?: { id: number; name: string } | null) {
  const options = CATALOG_GENRES.map((genre) => ({ id: genre.id, name: genre.name }))
  if (current && !options.some((genre) => genre.id === current.id)) {
    options.unshift({ id: current.id, name: current.name })
  }

  return options
}
