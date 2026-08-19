import { useEffect, useState } from 'react'
import { CartProvider } from './features/cart/CartProvider'
import { CartPage } from './features/cart/pages/CartPage'
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

  const page = path.replace(/\/$/, '') === '/cart' ? 'cart' : 'home'

  return (
    <CartProvider>
      <SiteHeader />
      {page === 'cart' ? <CartPage /> : <HomePage />}
    </CartProvider>
  )
}
