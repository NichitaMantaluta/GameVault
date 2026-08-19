import { useState } from 'react'
import { navigate } from '../../../app/navigation'
import { useAuth } from '../../auth/AuthProvider'
import { useCart } from '../CartProvider'
import type { GetCartItemResponse } from '../types/cart'
import './CartPage.css'

const priceFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
})

const coverTones = ['#1f4d4a', '#3d2b56', '#4a3728', '#1e3a5f', '#4a2c2a', '#2d4a1f']

export function CartPage() {
  const { isReady, isAuthenticated, login } = useAuth()
  const { cart, isLoading, error, reload, removeItem } = useCart()

  if (!isReady) {
    return (
      <main className="cart-page">
        <p className="cart-page__status">Loading cart…</p>
      </main>
    )
  }

  if (!isAuthenticated) {
    return (
      <main className="cart-page">
        <h1 className="cart-page__title">Your cart</h1>
        <div className="cart-page__message">
          <p>Log in to view and update your cart.</p>
          <button type="button" className="cart-page__button cart-page__button--primary" onClick={login}>
            Log in
          </button>
        </div>
      </main>
    )
  }

  return (
    <main className="cart-page">
      <header className="cart-page__header">
        <h1 className="cart-page__title">Your cart</h1>
        <button type="button" className="cart-page__link" onClick={() => navigate('/')}>
          Continue browsing
        </button>
      </header>

      {isLoading && cart.items.length === 0 && !error ? (
        <p className="cart-page__status">Loading cart…</p>
      ) : null}

      {error ? (
        <div className="cart-page__message" role="alert">
          <p>{error}</p>
          <button type="button" className="cart-page__button cart-page__button--primary" onClick={reload}>
            Try again
          </button>
        </div>
      ) : null}

      {!error && !isLoading && cart.items.length === 0 ? (
        <div className="cart-page__message">
          <p>Your cart is empty.</p>
          <button type="button" className="cart-page__button cart-page__button--primary" onClick={() => navigate('/')}>
            Browse games
          </button>
        </div>
      ) : null}

      {!error && cart.items.length > 0 ? (
        <>
          <ul className="cart-page__list">
            {cart.items.map((item) => (
              <CartLineItem key={item.gameId} item={item} disabled={isLoading} onRemove={removeItem} />
            ))}
          </ul>
          <p className="cart-page__subtotal">
            Subtotal <strong>{priceFormatter.format(cart.subtotal)}</strong>
          </p>
        </>
      ) : null}
    </main>
  )
}

type CartLineItemProps = {
  item: GetCartItemResponse
  disabled: boolean
  onRemove: (gameId: string) => Promise<void>
}

function CartLineItem({ item, disabled, onRemove }: CartLineItemProps) {
  const [imageFailed, setImageFailed] = useState(false)
  const [isRemoving, setIsRemoving] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const showImage = Boolean(item.imageUrl) && !imageFailed
  const busy = disabled || isRemoving

  async function handleRemove() {
    setIsRemoving(true)
    setActionError(null)
    try {
      await onRemove(item.gameId)
    } catch (cause) {
      setActionError(cause instanceof Error ? cause.message : 'Failed to remove item.')
      setIsRemoving(false)
    }
  }

  return (
    <li className="cart-item">
      <div
        className="cart-item__cover"
        style={showImage ? undefined : { backgroundColor: coverTone(item.name) }}
      >
        {showImage ? (
          <img
            className="cart-item__image"
            src={item.imageUrl ?? undefined}
            alt=""
            onError={() => setImageFailed(true)}
          />
        ) : (
          <span className="cart-item__initial" aria-hidden="true">
            {initial(item.name)}
          </span>
        )}
      </div>
      <div className="cart-item__details">
        <h2 className="cart-item__name">{item.name}</h2>
        <p className="cart-item__price">{priceFormatter.format(item.price)}</p>
        <div className="cart-item__actions">
          <button
            type="button"
            className="cart-page__button"
            onClick={() => void handleRemove()}
            disabled={busy}
          >
            Remove
          </button>
        </div>
        {actionError ? (
          <p className="cart-item__error" role="alert">
            {actionError}
          </p>
        ) : null}
      </div>
    </li>
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
