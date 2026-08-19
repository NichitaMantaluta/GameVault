import { navigate } from '../../app/navigation'
import { LoginButton } from '../auth/LoginButton'
import { useCart } from '../cart/CartProvider'
import { CartBadge } from '../cart/components/CartBadge'
import './SiteHeader.css'

export function SiteHeader() {
  const { error, reload } = useCart()

  return (
    <header className="site-header">
      <button type="button" className="site-header__brand" onClick={() => navigate('/')}>
        GameStore
      </button>
      <div className="site-header__actions">
        <CartBadge />
        <LoginButton />
      </div>
      {error ? (
        <p className="site-header__error" role="alert">
          {error}{' '}
          <button type="button" className="site-header__retry" onClick={reload}>
            Retry
          </button>
        </p>
      ) : null}
    </header>
  )
}
