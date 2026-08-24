import { useEffect, useState } from 'react'
import { AccountPage, type AccountSection } from './features/account/pages/AccountPage'
import { CartProvider } from './features/cart/CartProvider'
import { CartPage } from './features/cart/pages/CartPage'
import { GameDetailsPage } from './features/games/pages/GameDetailsPage'
import { HomePage } from './features/games/pages/HomePage'
import { SiteHeader } from './features/layout/SiteHeader'

type AccountRoute = {
  section: AccountSection
  orderId?: string
}

export default function App() {
  const [path, setPath] = useState(window.location.pathname)

  useEffect(() => {
    function handlePopState() {
      setPath(window.location.pathname)
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const normalizedPath = path.replace(/\/$/, '') || '/'
  const gameId = matchGameId(normalizedPath)
  const accountRoute = matchAccountRoute(normalizedPath)

  return (
    <CartProvider>
      <SiteHeader />
      {normalizedPath === '/cart' ? (
        <CartPage />
      ) : accountRoute ? (
        <AccountPage section={accountRoute.section} orderId={accountRoute.orderId} />
      ) : gameId ? (
        <GameDetailsPage gameId={gameId} />
      ) : (
        <HomePage />
      )}
    </CartProvider>
  )
}

function matchGameId(path: string): string | null {
  const match = /^\/games\/([^/]+)$/.exec(path)
  return match?.[1] ?? null
}

function matchAccountRoute(path: string): AccountRoute | null {
  if (path === '/account') {
    return { section: 'overview' }
  }

  if (path === '/account/orders') {
    return { section: 'orders' }
  }

  const orderMatch = /^\/account\/orders\/([^/]+)$/.exec(path)
  if (orderMatch?.[1]) {
    return { section: 'order', orderId: orderMatch[1] }
  }

  return null
}
