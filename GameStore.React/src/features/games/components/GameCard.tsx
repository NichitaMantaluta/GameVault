import { useState } from 'react'
import { useAuth } from '../../auth/AuthProvider'
import { useCart } from '../../cart/CartProvider'
import type { GetGamesItem } from '../types/games'
import './GameCard.css'

const priceFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
})

const coverTones = ['#1f4d4a', '#3d2b56', '#4a3728', '#1e3a5f', '#4a2c2a', '#2d4a1f']

type GameCardProps = {
  game: GetGamesItem
}

export function GameCard({ game }: GameCardProps) {
  const { isAuthenticated, login } = useAuth()
  const { addItem } = useCart()
  const [imageFailed, setImageFailed] = useState(false)
  const [isAdding, setIsAdding] = useState(false)
  const [addError, setAddError] = useState<string | null>(null)
  const [justAdded, setJustAdded] = useState(false)
  const showImage = Boolean(game.imageUrl) && !imageFailed

  async function handleAddToCart() {
    if (!isAuthenticated) {
      login()
      return
    }

    setIsAdding(true)
    setAddError(null)
    setJustAdded(false)

    try {
      await addItem(game.id)
      setJustAdded(true)
    } catch (cause) {
      setAddError(cause instanceof Error ? cause.message : 'Failed to add to cart.')
    } finally {
      setIsAdding(false)
    }
  }

  return (
    <article className="game-card">
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
      <div className="game-card__body">
        <p className="game-card__genre">{game.genreName}</p>
        <h2 className="game-card__name">{game.name}</h2>
        <p className="game-card__price">{priceFormatter.format(game.price)}</p>
        <button
          type="button"
          className="game-card__cart"
          onClick={() => void handleAddToCart()}
          disabled={isAdding}
        >
          {isAdding ? 'Adding…' : justAdded ? 'Added' : 'Add to cart'}
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
