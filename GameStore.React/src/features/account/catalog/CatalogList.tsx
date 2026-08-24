import { useEffect, useState } from 'react'
import { navigate } from '../../../app/navigation'
import { ADMIN_CATALOG_PAGE_SIZE, getGames } from '../../games/api/gamesApi'
import { Pagination } from '../../games/components/Pagination'
import { SearchInput } from '../../games/components/SearchInput'
import type { GetGamesItem, GetGamesResponse } from '../../games/types/games'
import { consumeCatalogFeedback } from './feedback'

const priceFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
})

const SEARCH_DELAY_MS = 400

type StatusFirst = 'active' | 'inactive'

export function CatalogList() {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [statusFirst, setStatusFirst] = useState<StatusFirst>('inactive')
  const [catalog, setCatalog] = useState<GetGamesResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)
  const [feedback] = useState<string | null>(() => consumeCatalogFeedback())

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setSearch(searchInput.trim())
      setPage(1)
    }, SEARCH_DELAY_MS)

    return () => window.clearTimeout(timeoutId)
  }, [searchInput])

  useEffect(() => {
    const controller = new AbortController()

    async function loadCatalog() {
      setIsLoading(true)
      setError(null)

      try {
        const response = await getGames(
          {
            page,
            pageSize: ADMIN_CATALOG_PAGE_SIZE,
            search: search.length > 0 ? search : undefined,
            includeInactive: true,
            statusFirst,
          },
          controller.signal,
        )
        setCatalog(response)
      } catch (cause) {
        if (isAbortError(cause)) {
          return
        }

        setCatalog(null)
        setError(cause instanceof Error ? cause.message : 'Failed to load catalog.')
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      }
    }

    void loadCatalog()

    return () => controller.abort()
  }, [page, search, statusFirst, reloadKey])

  function toggleStatusSort() {
    setStatusFirst((current) => (current === 'active' ? 'inactive' : 'active'))
    setPage(1)
  }

  return (
    <div className="catalog-admin">
      <div className="catalog-admin__header">
        <div>
          <h2 className="account-page__section-title">Game Catalog</h2>
          <p className="catalog-admin__subtitle">
            Active and disabled games are listed here. Disabled games appear first by default —
            click Status to put active games first.
          </p>
        </div>
        <button
          type="button"
          className="account-page__button account-page__button--primary"
          onClick={() => navigate('/account/catalog/new')}
        >
          New Game
        </button>
      </div>

      <div className="catalog-admin__toolbar">
        <SearchInput value={searchInput} onChange={setSearchInput} />
      </div>

      {feedback ? (
        <p className="catalog-admin__feedback" role="status">
          {feedback}
        </p>
      ) : null}

      {isLoading && !catalog ? <p className="account-page__status">Loading catalog…</p> : null}

      {error ? (
        <div className="account-page__message" role="alert">
          <p>{error}</p>
          <button
            type="button"
            className="account-page__button account-page__button--primary"
            onClick={() => setReloadKey((current) => current + 1)}
          >
            Try again
          </button>
        </div>
      ) : null}

      {!error && catalog && catalog.totalCount === 0 ? (
        <div className="account-page__empty">
          <p className="account-page__status">
            {search ? `No games matched “${search}”.` : 'The catalog is empty.'}
          </p>
          <button
            type="button"
            className="account-page__button account-page__button--primary"
            onClick={() => navigate('/account/catalog/new')}
          >
            Add a game
          </button>
        </div>
      ) : null}

      {!error && catalog && catalog.items.length > 0 ? (
        <>
          <p className="catalog-admin__count">
            {catalog.totalCount} {catalog.totalCount === 1 ? 'game' : 'games'}
            {search ? ` matching “${search}”` : ''}
            {` · ${statusFirst === 'active' ? 'Active first' : 'Disabled first'}`}
          </p>
          <div className="catalog-admin__table-wrap" aria-busy={isLoading}>
            <table className="catalog-admin__table">
              <thead>
                <tr>
                  <th scope="col">Game</th>
                  <th scope="col">Genre</th>
                  <th scope="col">Price</th>
                  <th scope="col">
                    <button
                      type="button"
                      className="catalog-admin__sort"
                      onClick={toggleStatusSort}
                      aria-pressed={statusFirst === 'inactive'}
                      title="Toggle active or disabled first"
                    >
                      Status
                      <span aria-hidden="true">{statusFirst === 'active' ? ' ↑' : ' ↓'}</span>
                    </button>
                  </th>
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {catalog.items.map((game) => (
                  <CatalogRow key={game.id} game={game} />
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={catalog.page}
            totalPages={catalog.totalPages}
            onPageChange={setPage}
          />
        </>
      ) : null}
    </div>
  )
}

function CatalogRow({ game }: { game: GetGamesItem }) {
  // Older API builds omit isActive; those list responses are active-only.
  const isActive = game.isActive !== false

  return (
    <tr className={isActive ? undefined : 'catalog-admin__row--inactive'}>
      <td>
        <div className="catalog-admin__game">
          <CatalogThumb name={game.name} imageUrl={game.imageUrl} />
          <div>
            <p className="catalog-admin__name">{game.name}</p>
            <p className="catalog-admin__id">{game.id}</p>
          </div>
        </div>
      </td>
      <td>{game.genreName}</td>
      <td>{priceFormatter.format(game.price)}</td>
      <td>
        <span
          className={
            isActive
              ? 'catalog-admin__badge catalog-admin__badge--active'
              : 'catalog-admin__badge catalog-admin__badge--inactive'
          }
        >
          {isActive ? 'Active' : 'Disabled'}
        </span>
      </td>
      <td>
        <div className="catalog-admin__row-actions">
          <button
            type="button"
            className="account-page__button"
            onClick={() => navigate(`/account/catalog/${game.id}/edit`)}
          >
            Edit
          </button>
          {isActive ? (
            <button
              type="button"
              className="account-page__button catalog-admin__danger"
              onClick={() => navigate(`/account/catalog/${game.id}/edit?focus=disable`)}
            >
              Disable
            </button>
          ) : null}
        </div>
      </td>
    </tr>
  )
}

function CatalogThumb({ name, imageUrl }: { name: string; imageUrl: string | null }) {
  const [failed, setFailed] = useState(false)
  const showImage = Boolean(imageUrl) && !failed

  return (
    <div className="catalog-admin__thumb" aria-hidden="true">
      {showImage ? (
        <img src={imageUrl ?? undefined} alt="" onError={() => setFailed(true)} />
      ) : (
        <span>{name.trim().charAt(0).toUpperCase() || '?'}</span>
      )}
    </div>
  )
}

function isAbortError(cause: unknown): boolean {
  return cause instanceof Error && cause.name === 'AbortError'
}
