import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { useAuth } from '../auth/AuthProvider'
import { getOwnedGames } from '../orders/api/ordersApi'
import { addCartItem, emptyCart, getCart, removeCartItem } from './api/cartApi'
import type { GetCartResponse } from './types/cart'

type CartContextValue = {
  cart: GetCartResponse
  itemCount: number
  isLoading: boolean
  error: string | null
  reload: () => void
  containsGame: (gameId: string) => boolean
  ownsGame: (gameId: string) => boolean
  addItem: (gameId: string) => Promise<void>
  removeItem: (gameId: string) => Promise<void>
}

const CartContext = createContext<CartContextValue | null>(null)

export function CartProvider({ children }: { children: ReactNode }) {
  const { isReady, isAuthenticated } = useAuth()
  const [cart, setCart] = useState<GetCartResponse>(emptyCart)
  const [ownedGameIds, setOwnedGameIds] = useState<ReadonlySet<string>>(() => new Set())
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    if (!isReady) {
      return
    }

    if (!isAuthenticated) {
      setCart(emptyCart)
      setOwnedGameIds(new Set())
      setError(null)
      setIsLoading(false)
      return
    }

    const controller = new AbortController()

    async function loadCartAndLibrary() {
      setIsLoading(true)
      setError(null)

      try {
        const [cartResponse, ownedResponse] = await Promise.all([
          getCart(controller.signal),
          getOwnedGames(controller.signal),
        ])
        setCart(cartResponse)
        setOwnedGameIds(new Set(ownedResponse.gameIds))
      } catch (cause) {
        if (isAbortError(cause)) {
          return
        }

        setCart(emptyCart)
        setOwnedGameIds(new Set())
        setError(cause instanceof Error ? cause.message : 'Failed to load cart.')
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      }
    }

    void loadCartAndLibrary()

    return () => controller.abort()
  }, [isAuthenticated, isReady, reloadKey])

  const addItem = useCallback(async (gameId: string) => {
    const response = await addCartItem({ gameId })
    setCart(response)
    setError(null)
  }, [])

  const removeItem = useCallback(async (gameId: string) => {
    await removeCartItem(gameId)
    const response = await getCart()
    setCart(response)
    setError(null)
  }, [])

  const value = useMemo<CartContextValue>(
    () => ({
      cart,
      itemCount: cart.items.length,
      isLoading,
      error,
      reload: () => setReloadKey((current) => current + 1),
      containsGame: (gameId: string) => cart.items.some((item) => item.gameId === gameId),
      ownsGame: (gameId: string) => ownedGameIds.has(gameId),
      addItem,
      removeItem,
    }),
    [addItem, cart, error, isLoading, ownedGameIds, removeItem],
  )

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}

export function useCart() {
  const context = useContext(CartContext)
  if (!context) {
    throw new Error('useCart must be used within CartProvider.')
  }

  return context
}

function isAbortError(cause: unknown): boolean {
  return cause instanceof Error && cause.name === 'AbortError'
}
