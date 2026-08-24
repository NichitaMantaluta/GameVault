import { useEffect, useState } from 'react'
import { navigate } from '../../../app/navigation'
import {
  disableGame,
  GameNotFoundError,
  getGame,
  updateGame,
} from '../../games/api/gamesApi'
import type { GetGameResponse } from '../../games/types/games'
import { setCatalogFeedback } from './feedback'
import { GameForm, type GameFormValues } from './GameForm'

type CatalogEditProps = {
  gameId: string
  focusDisable?: boolean
}

export function CatalogEdit({ gameId, focusDisable = false }: CatalogEditProps) {
  const [game, setGame] = useState<GetGameResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)
  const [confirmDisable, setConfirmDisable] = useState(focusDisable)
  const [isDisabling, setIsDisabling] = useState(false)
  const [disableError, setDisableError] = useState<string | null>(null)

  const [isReactivating, setIsReactivating] = useState(false)
  const [reactivateError, setReactivateError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()

    async function loadGame() {
      setIsLoading(true)
      setError(null)
      setNotFound(false)
      setGame(null)

      try {
        const response = await getGame(gameId, controller.signal)
        setGame(response)
      } catch (cause) {
        if (isAbortError(cause)) {
          return
        }

        if (cause instanceof GameNotFoundError) {
          setNotFound(true)
          return
        }

        setError(cause instanceof Error ? cause.message : 'Failed to load game.')
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      }
    }

    void loadGame()

    return () => controller.abort()
  }, [gameId, reloadKey])

  async function handleSubmit(values: GameFormValues) {
    if (!game) {
      return
    }

    const updated = await updateGame(game.id, toUpdatePayload(values))
    const wasReactivated = !game.isActive && updated.isActive
    setCatalogFeedback(
      wasReactivated ? `Reactivated “${updated.name}”.` : `Updated “${updated.name}”.`,
    )
    navigate('/account/catalog')
  }

  async function handleDisable() {
    if (!game) {
      return
    }

    setIsDisabling(true)
    setDisableError(null)

    try {
      await disableGame(game.id)
      setCatalogFeedback(`Disabled “${game.name}”.`)
      navigate('/account/catalog')
    } catch (cause) {
      setDisableError(cause instanceof Error ? cause.message : 'Failed to disable game.')
      setIsDisabling(false)
    }
  }

  async function handleReactivate() {
    if (!game) {
      return
    }

    setIsReactivating(true)
    setReactivateError(null)

    try {
      const updated = await updateGame(game.id, {
        name: game.name,
        description: game.description,
        price: game.price,
        genreId: game.genreId,
        imageUrl: game.imageUrl,
        isActive: true,
      })
      setCatalogFeedback(`Reactivated “${updated.name}”.`)
      navigate('/account/catalog')
    } catch (cause) {
      setReactivateError(cause instanceof Error ? cause.message : 'Failed to reactivate game.')
      setIsReactivating(false)
    }
  }

  return (
    <div>
      <button
        type="button"
        className="account-page__link account-page__back"
        onClick={() => navigate('/account/catalog')}
      >
        ← Back to catalog
      </button>

      {isLoading ? <p className="account-page__status">Loading game…</p> : null}

      {notFound ? (
        <div className="account-page__message" role="alert">
          <p>This game was not found.</p>
          <button
            type="button"
            className="account-page__button account-page__button--primary"
            onClick={() => navigate('/account/catalog')}
          >
            Back to catalog
          </button>
        </div>
      ) : null}

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

      {!isLoading && !error && !notFound && game ? (
        <>
          <GameForm
            title={`Edit ${game.name}`}
            submitLabel="Save changes"
            initialValues={toFormValues(game)}
            currentGenre={{ id: game.genreId, name: game.genreName }}
            showActiveToggle
            onSubmit={handleSubmit}
            onCancel={() => navigate('/account/catalog')}
          />

          {game.isActive ? (
            <section className="catalog-admin__disable" aria-label="Disable game">
              <h3 className="catalog-admin__disable-title">Disable game</h3>
              <p className="catalog-admin__subtitle">
                Soft-deactivates this game so it no longer appears in the customer catalog. Order
                history is kept.
              </p>

              {!confirmDisable ? (
                <button
                  type="button"
                  className="account-page__button catalog-admin__danger"
                  onClick={() => setConfirmDisable(true)}
                >
                  Disable
                </button>
              ) : (
                <div className="catalog-admin__confirm" role="group" aria-label="Confirm disable">
                  <p>
                    Disable <strong>{game.name}</strong>? Customers will no longer see it in the
                    catalog.
                  </p>
                  <div className="catalog-form__actions">
                    <button
                      type="button"
                      className="account-page__button catalog-admin__danger"
                      onClick={() => void handleDisable()}
                      disabled={isDisabling}
                    >
                      {isDisabling ? 'Disabling…' : 'Confirm disable'}
                    </button>
                    <button
                      type="button"
                      className="account-page__button"
                      onClick={() => {
                        setConfirmDisable(false)
                        setDisableError(null)
                      }}
                      disabled={isDisabling}
                    >
                      Cancel
                    </button>
                  </div>
                  {disableError ? (
                    <p className="catalog-form__error" role="alert">
                      {disableError}
                    </p>
                  ) : null}
                </div>
              )}
            </section>
          ) : (
            <section className="catalog-admin__disable" aria-label="Reactivate game">
              <h3 className="catalog-admin__disable-title">Reactivate game</h3>
              <p className="catalog-admin__subtitle">
                This game is disabled and hidden from the customer catalog. Reactivate it to make
                it available again.
              </p>
              <button
                type="button"
                className="account-page__button account-page__button--primary"
                onClick={() => void handleReactivate()}
                disabled={isReactivating}
              >
                {isReactivating ? 'Reactivating…' : 'Reactivate'}
              </button>
              {reactivateError ? (
                <p className="catalog-form__error" role="alert">
                  {reactivateError}
                </p>
              ) : null}
            </section>
          )}
        </>
      ) : null}
    </div>
  )
}

function toFormValues(game: GetGameResponse): GameFormValues {
  return {
    name: game.name,
    description: game.description,
    price: String(game.price),
    genreId: String(game.genreId),
    imageUrl: game.imageUrl ?? '',
    isActive: game.isActive,
  }
}

function toUpdatePayload(values: GameFormValues) {
  const price = Number(values.price)
  const genreId = Number(values.genreId)
  const imageUrl = values.imageUrl.trim()

  return {
    name: values.name.trim(),
    description: values.description.trim(),
    price: Number.isFinite(price) ? price : 0,
    genreId: Number.isFinite(genreId) ? genreId : 0,
    isActive: values.isActive,
    imageUrl: imageUrl.length > 0 ? imageUrl : null,
  }
}

function isAbortError(cause: unknown): boolean {
  return cause instanceof Error && cause.name === 'AbortError'
}
