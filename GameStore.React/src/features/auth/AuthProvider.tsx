import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { getKeycloak, initKeycloak, loginToStore, logoutFromStore } from './keycloak'

type AuthContextValue = {
  isReady: boolean
  isAuthenticated: boolean
  username: string | null
  login: () => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isReady, setIsReady] = useState(false)
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [username, setUsername] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    void initKeycloak()
      .then((authenticated) => {
        if (cancelled) {
          return
        }

        const parsed = getKeycloak().tokenParsed
        setIsAuthenticated(authenticated)
        setUsername(
          typeof parsed?.preferred_username === 'string'
            ? parsed.preferred_username
            : null,
        )
        setIsReady(true)
      })
      .catch(() => {
        if (!cancelled) {
          setIsReady(true)
        }
      })

    return () => {
      cancelled = true
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      isReady,
      isAuthenticated,
      username,
      login: () => {
        void loginToStore()
      },
      logout: () => {
        void logoutFromStore()
      },
    }),
    [isAuthenticated, isReady, username],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider.')
  }

  return context
}
