import { useEffect, useState } from 'react'
import { navigate } from '../../../app/navigation'
import { useAuth } from '../../auth/AuthProvider'
import { useCart } from '../../cart/CartProvider'
import { GameNotFoundError, getGame } from '../api/gamesApi'
import type { GetGameResponse } from '../types/games'
import './GameDetailsPage.css'

const priceFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
})

const coverTones = ['#1f4d4a', '#3d2b56', '#4a3728', '#1e3a5f', '#4a2c2a', '#2d4a1f']

type GameDetailsPageProps = {
  gameId: string
}

export function GameDetailsPage({ gameId }: GameDetailsPageProps) {
  const { isAuthenticated, login } = useAuth()
  const { addItem, containsGame } = useCart()
  const [game, setGame] = useState<GetGameResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)
  const [imageFailed, setImageFailed] = useState(false)
  const [isAdding, setIsAdding] = useState(false)
  const [addError, setAddError] = useState<string | null>(null)
  const [addSuccess, setAddSuccess] = useState(false)

  useEffect(() => {
    const controller = new AbortController()

    async function loadGame() {
      setIsLoading(true)
      setError(null)
      setNotFound(false)
      setGame(null)
      setImageFailed(false)
      setAddError(null)
      setAddSuccess(false)

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

  const inCart = game ? containsGame(game.id) : false
  const showImage = Boolean(game?.imageUrl) && !imageFailed

  async function handleCartClick() {
    if (!game) {
      return
    }

    if (!isAuthenticated) {
      login()
      return
    }

    if (inCart) {
      navigate('/cart')
      return
    }

    setIsAdding(true)
    setAddError(null)
    setAddSuccess(false)

    try {
      await addItem(game.id)
      setAddSuccess(true)
    } catch (cause) {
      setAddError(cause instanceof Error ? cause.message : 'Failed to add to cart.')
    } finally {
      setIsAdding(false)
    }
  }

  return (
    <main className="game-details">
      <button type="button" className="game-details__back" onClick={() => navigate('/')}>
        ← Back to Catalog
      </button>

      {isLoading ? <p className="game-details__status">Loading game…</p> : null}

      {notFound ? (
        <div className="game-details__message" role="alert">
          <h1 className="game-details__message-title">Game not found</h1>
          <p>This game is unavailable or the link is incorrect.</p>
          <button
            type="button"
            className="game-details__button game-details__button--primary"
            onClick={() => navigate('/')}
          >
            Browse games
          </button>
        </div>
      ) : null}

      {error ? (
        <div className="game-details__message" role="alert">
          <h1 className="game-details__message-title">Couldn’t load game</h1>
          <p>{error}</p>
          <button
            type="button"
            className="game-details__button game-details__button--primary"
            onClick={() => setReloadKey((current) => current + 1)}
          >
            Try again
          </button>
        </div>
      ) : null}

      {!isLoading && !error && !notFound && game ? (
        <article className="game-details__product">
          <div
            className="game-details__cover"
            style={showImage ? undefined : { backgroundColor: coverTone(game.name) }}
          >
            {showImage ? (
              <img
                className="game-details__image"
                src={game.imageUrl ?? undefined}
                alt=""
                onError={() => setImageFailed(true)}
              />
            ) : (
              <span className="game-details__initial" aria-hidden="true">
                {initial(game.name)}
              </span>
            )}
          </div>

          <div className="game-details__info">
            <p className="game-details__genre">{game.genreName}</p>
            <h1 className="game-details__name">{game.name}</h1>
            <p className="game-details__price">{priceFormatter.format(game.price)}</p>

            {game.description.trim() ? (
              <p className="game-details__description">{game.description}</p>
            ) : (
              <p className="game-details__description game-details__description--empty">
                No description available.
              </p>
            )}

            <div className="game-details__actions">
              <button
                type="button"
                className={
                  inCart
                    ? 'game-details__button game-details__button--secondary'
                    : 'game-details__button game-details__button--primary'
                }
                onClick={() => void handleCartClick()}
                disabled={isAdding || (!inCart && !game.isActive)}
              >
                {isAdding ? 'Adding…' : inCart ? 'In cart' : 'Add to cart'}
              </button>
              {addSuccess || inCart ? (
                <p className="game-details__feedback" role="status">
                  {inCart
                    ? 'This game is in your cart.'
                    : 'Added to cart.'}
                </p>
              ) : null}
              {addError ? (
                <p className="game-details__error" role="alert">
                  {addError}
                </p>
              ) : null}
            </div>
          </div>
        </article>
      ) : null}
    </main>
  )
}

function initial(name: string): string {
  const trimmed = name.trim()
  return trimmed.length > 0 ? trimmed[0].toUpperCase() : '?'
}

function coverTone(name: string): string {
  let hash = 0
  for (const character of name) {
    hash = (hash + character.charCodeAt(0)) % coverTones.length
  }
  return coverTones[hash] ?? coverTones[0]
}

function isAbortError(cause: unknown): boolean {
  return cause instanceof Error && cause.name === 'AbortError'
}
