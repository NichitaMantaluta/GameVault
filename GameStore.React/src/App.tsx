import { useEffect, useState } from 'react'
import { CartProvider } from './features/cart/CartProvider'
import { CartPage } from './features/cart/pages/CartPage'
import { GameDetailsPage } from './features/games/pages/GameDetailsPage'
import { HomePage } from './features/games/pages/HomePage'
import { SiteHeader } from './features/layout/SiteHeader'

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

  return (
    <CartProvider>
      <SiteHeader />
      {normalizedPath === '/cart' ? (
        <CartPage />
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
