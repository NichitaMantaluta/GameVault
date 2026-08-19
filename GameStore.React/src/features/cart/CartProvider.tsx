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
import { addCartItem, emptyCart, getCart, removeCartItem } from './api/cartApi'
import type { GetCartResponse } from './types/cart'

type CartContextValue = {
  cart: GetCartResponse
  itemCount: number
  isLoading: boolean
  error: string | null
  reload: () => void
  containsGame: (gameId: string) => boolean
  addItem: (gameId: string) => Promise<void>
  removeItem: (gameId: string) => Promise<void>
}

const CartContext = createContext<CartContextValue | null>(null)

export function CartProvider({ children }: { children: ReactNode }) {
  const { isReady, isAuthenticated } = useAuth()
  const [cart, setCart] = useState<GetCartResponse>(emptyCart)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    if (!isReady) {
      return
    }

    if (!isAuthenticated) {
      setCart(emptyCart)
      setError(null)
      setIsLoading(false)
      return
    }

    const controller = new AbortController()

    async function loadCart() {
      setIsLoading(true)
      setError(null)

      try {
        const response = await getCart(controller.signal)
        setCart(response)
      } catch (cause) {
        if (isAbortError(cause)) {
          return
        }

        setCart(emptyCart)
        setError(cause instanceof Error ? cause.message : 'Failed to load cart.')
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      }
    }

    void loadCart()

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
      addItem,
      removeItem,
    }),
    [addItem, cart, error, isLoading, removeItem],
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
