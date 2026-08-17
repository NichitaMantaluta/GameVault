import { useEffect, useState } from 'react'
import { DEFAULT_PAGE_SIZE, getGames } from '../api/gamesApi'
import { GameCard } from '../components/GameCard'
import { Pagination } from '../components/Pagination'
import { SearchInput } from '../components/SearchInput'
import type { GetGamesResponse } from '../types/games'
import './HomePage.css'

const SEARCH_DELAY_MS = 400

export function HomePage() {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [catalog, setCatalog] = useState<GetGamesResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setSearch(searchInput.trim())
    }, SEARCH_DELAY_MS)

    return () => window.clearTimeout(timeoutId)
  }, [searchInput])

  useEffect(() => {
    const controller = new AbortController()

    async function loadGames() {
      setIsLoading(true)
      setError(null)

      try {
        const response = await getGames(
          {
            page,
            pageSize: DEFAULT_PAGE_SIZE,
            search: search.length > 0 ? search : undefined,
          },
          controller.signal,
        )
        setCatalog(response)
      } catch (cause) {
        if (isAbortError(cause)) {
          return
        }

        setCatalog(null)
        setError(cause instanceof Error ? cause.message : 'Failed to load games.')
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      }
    }

    void loadGames()

    return () => controller.abort()
  }, [page, search, reloadKey])

  function handleSearchChange(value: string) {
    setSearchInput(value)
    setPage(1)
  }

  return (
    <main className="home">
      <header className="home__header">
        <p className="home__eyebrow">GameStore</p>
        <h1 className="home__title">Browse the catalog</h1>
        <p className="home__subtitle">Find games by name across the store.</p>
        <SearchInput value={searchInput} onChange={handleSearchChange} />
      </header>

      {isLoading && !catalog ? <p className="home__status">Loading games…</p> : null}

      {error ? (
        <div className="home__message" role="alert">
          <p>{error}</p>
          <button
            type="button"
            className="home__retry"
            onClick={() => setReloadKey((current) => current + 1)}
          >
            Try again
          </button>
        </div>
      ) : null}

      {!error && catalog ? (
        <CatalogResults
          catalog={catalog}
          search={search}
          isLoading={isLoading}
          onPageChange={setPage}
        />
      ) : null}
    </main>
  )
}

type CatalogResultsProps = {
  catalog: GetGamesResponse
  search: string
  isLoading: boolean
  onPageChange: (page: number) => void
}

function CatalogResults({
  catalog,
  search,
  isLoading,
  onPageChange,
}: CatalogResultsProps) {
  if (catalog.totalCount === 0) {
    return (
      <p className="home__status">
        {search
          ? `No games matched “${search}”.`
          : 'The catalog is empty.'}
      </p>
    )
  }

  if (catalog.items.length === 0) {
    return (
      <>
        <p className="home__status">No games on this page.</p>
        <Pagination
          page={catalog.page}
          totalPages={catalog.totalPages}
          onPageChange={onPageChange}
        />
      </>
    )
  }

  return (
    <section aria-busy={isLoading} aria-live="polite">
      <p className="home__count">
        {catalog.totalCount} {catalog.totalCount === 1 ? 'game' : 'games'}
        {search ? ` matching “${search}”` : ''}
      </p>
      <div className="home__grid">
        {catalog.items.map((game) => (
          <GameCard key={game.id} game={game} />
        ))}
      </div>
      <Pagination
        page={catalog.page}
        totalPages={catalog.totalPages}
        onPageChange={onPageChange}
      />
    </section>
  )
}

function isAbortError(cause: unknown): boolean {
  return cause instanceof Error && cause.name === 'AbortError'
}
