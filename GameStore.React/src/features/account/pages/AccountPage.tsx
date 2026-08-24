import { navigate } from '../../../app/navigation'
import { useAuth } from '../../auth/AuthProvider'
import { AccountOverview } from '../components/AccountOverview'
import { OrderDetails } from '../components/OrderDetails'
import { OrderHistory } from '../components/OrderHistory'
import { CatalogCreate } from '../catalog/CatalogCreate'
import { CatalogEdit } from '../catalog/CatalogEdit'
import { CatalogList } from '../catalog/CatalogList'
import './AccountPage.css'

export type AccountSection =
  | 'overview'
  | 'orders'
  | 'order'
  | 'catalog'
  | 'catalog-new'
  | 'catalog-edit'

type AccountPageProps = {
  section: AccountSection
  orderId?: string
  catalogGameId?: string
  focusDisable?: boolean
}

export function AccountPage({
  section,
  orderId,
  catalogGameId,
  focusDisable = false,
}: AccountPageProps) {
  const { isReady, isAuthenticated, isAdmin, login } = useAuth()
  const ordersActive = section === 'orders' || section === 'order'
  const catalogActive =
    section === 'catalog' || section === 'catalog-new' || section === 'catalog-edit'

  if (!isReady) {
    return (
      <main className="account-page">
        <p className="account-page__status">Loading account…</p>
      </main>
    )
  }

  if (!isAuthenticated) {
    return (
      <main className="account-page">
        <h1 className="account-page__title">Account</h1>
        <div className="account-page__message">
          <p>Log in to view your account and order history.</p>
          <button
            type="button"
            className="account-page__button account-page__button--primary"
            onClick={login}
          >
            Log in
          </button>
        </div>
      </main>
    )
  }

  if (catalogActive && !isAdmin) {
    return (
      <main className="account-page">
        <h1 className="account-page__title">Account</h1>
        <div className="account-page__message" role="alert">
          <p>You do not have permission to manage the game catalog.</p>
          <button
            type="button"
            className="account-page__button account-page__button--primary"
            onClick={() => navigate('/account')}
          >
            Back to account
          </button>
        </div>
      </main>
    )
  }

  return (
    <main className="account-page">
      <h1 className="account-page__title">Account</h1>
      <div className="account-page__layout">
        <nav className="account-page__nav" aria-label="Account">
          <button
            type="button"
            className={
              section === 'overview'
                ? 'account-page__nav-item account-page__nav-item--active'
                : 'account-page__nav-item'
            }
            onClick={() => navigate('/account')}
            aria-current={section === 'overview' ? 'page' : undefined}
          >
            Overview
          </button>
          <button
            type="button"
            className={
              ordersActive
                ? 'account-page__nav-item account-page__nav-item--active'
                : 'account-page__nav-item'
            }
            onClick={() => navigate('/account/orders')}
            aria-current={ordersActive ? 'page' : undefined}
          >
            Order History
          </button>
          {isAdmin ? (
            <button
              type="button"
              className={
                catalogActive
                  ? 'account-page__nav-item account-page__nav-item--active'
                  : 'account-page__nav-item'
              }
              onClick={() => navigate('/account/catalog')}
              aria-current={catalogActive ? 'page' : undefined}
            >
              Game Catalog
            </button>
          ) : null}
        </nav>
        <section className="account-page__content" aria-live="polite">
          {section === 'overview' ? <AccountOverview /> : null}
          {section === 'orders' ? <OrderHistory /> : null}
          {section === 'order' && orderId ? <OrderDetails orderId={orderId} /> : null}
          {section === 'catalog' ? <CatalogList /> : null}
          {section === 'catalog-new' ? <CatalogCreate /> : null}
          {section === 'catalog-edit' && catalogGameId ? (
            <CatalogEdit gameId={catalogGameId} focusDisable={focusDisable} />
          ) : null}
        </section>
      </div>
    </main>
  )
}
