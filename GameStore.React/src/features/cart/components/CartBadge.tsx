import { navigate } from '../../../app/navigation'
import { useCart } from '../CartProvider'
import './CartBadge.css'

export function CartBadge() {
  const { itemCount } = useCart()

  return (
    <button
      type="button"
      className="cart-badge"
      onClick={() => navigate('/cart')}
      aria-label={label(itemCount)}
    >
      <span>Cart</span>
      <span className="cart-badge__count" aria-hidden="true">
        {itemCount}
      </span>
    </button>
  )
}

function label(itemCount: number): string {
  if (itemCount === 1) {
    return 'Cart, 1 item'
  }

  return `Cart, ${itemCount} items`
}
