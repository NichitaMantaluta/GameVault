import { useState } from 'react'
import { navigate } from '../../../app/navigation'
import { formatUsd } from '../../account/format'
import { useAuth } from '../../auth/AuthProvider'
import { useCart } from '../../cart/CartProvider'
import type { GetGamesItem } from '../types/games'
import './GameCard.css'

const coverTones = ['#1f4d4a', '#3d2b56', '#4a3728', '#1e3a5f', '#4a2c2a', '#2d4a1f']

type GameCardProps = {
  game: GetGamesItem
}

export function GameCard({ game }: GameCardProps) {
  const { isAuthenticated, login } = useAuth()
  const { addItem, containsGame, ownsGame } = useCart()
  const [imageFailed, setImageFailed] = useState(false)
  const [isAdding, setIsAdding] = useState(false)
  const [addError, setAddError] = useState<string | null>(null)
  const showImage = Boolean(game.imageUrl) && !imageFailed
  const owned = ownsGame(game.id)
  const inCart = containsGame(game.id)
  const detailsPath = `/games/${game.id}`

  function openDetails() {
    navigate(detailsPath)
  }

  async function handleCartClick() {
    if (!isAuthenticated) {
      login()
      return
    }

    if (owned) {
      return
    }

    if (inCart) {
      navigate('/cart')
      return
    }

    setIsAdding(true)
    setAddError(null)

    try {
      await addItem(game.id)
    } catch (cause) {
      setAddError(cause instanceof Error ? cause.message : 'Failed to add to cart.')
    } finally {
      setIsAdding(false)
    }
  }

  const cartLabel = owned ? 'Owned' : isAdding ? 'Adding…' : inCart ? 'In cart' : 'Add to cart'
  const cartClassName = owned
    ? 'game-card__cart game-card__cart--owned'
    : inCart
      ? 'game-card__cart game-card__cart--in-cart'
      : 'game-card__cart'

  return (
    <article className="game-card">
      <button
        type="button"
        className="game-card__cover-link"
        onClick={openDetails}
        aria-label={`View details for ${game.name}`}
      >
        <div
          className="game-card__cover"
          style={showImage ? undefined : { backgroundColor: coverTone(game.name) }}
        >
          {showImage ? (
            <img
              className="game-card__image"
              src={game.imageUrl ?? undefined}
              alt=""
              onError={() => setImageFailed(true)}
            />
          ) : (
            <span className="game-card__initial" aria-hidden="true">
              {initial(game.name)}
            </span>
          )}
        </div>
      </button>
      <div className="game-card__body">
        <p className="game-card__genre">{game.genreName}</p>
        <h2 className="game-card__name">
          <button type="button" className="game-card__name-link" onClick={openDetails}>
            {game.name}
          </button>
        </h2>
        <p className="game-card__price">{formatUsd(game.price)}</p>
        <button
          type="button"
          className={cartClassName}
          onClick={() => void handleCartClick()}
          disabled={owned || isAdding}
        >
          {cartLabel}
        </button>
        {addError ? (
          <p className="game-card__error" role="alert">
            {addError}
          </p>
        ) : null}
      </div>
    </article>
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
