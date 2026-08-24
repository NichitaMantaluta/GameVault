import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { getKeycloak, initKeycloak, loginToStore, logoutFromStore } from './keycloak'

export type AccountProfile = {
  username: string | null
  email: string | null
  name: string | null
  givenName: string | null
  familyName: string | null
  subject: string | null
  emailVerified: boolean | null
}

type AuthContextValue = {
  isReady: boolean
  isAuthenticated: boolean
  username: string | null
  profile: AccountProfile
  login: () => void
  logout: () => void
}

const emptyProfile: AccountProfile = {
  username: null,
  email: null,
  name: null,
  givenName: null,
  familyName: null,
  subject: null,
  emailVerified: null,
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isReady, setIsReady] = useState(false)
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [profile, setProfile] = useState<AccountProfile>(emptyProfile)

  useEffect(() => {
    let cancelled = false

    void initKeycloak()
      .then((authenticated) => {
        if (cancelled) {
          return
        }

        setIsAuthenticated(authenticated)
        setProfile(authenticated ? readProfile(getKeycloak().tokenParsed) : emptyProfile)
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
      username: profile.username,
      profile,
      login: () => {
        void loginToStore()
      },
      logout: () => {
        void logoutFromStore()
      },
    }),
    [isAuthenticated, isReady, profile],
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

function readProfile(parsed: Record<string, unknown> | undefined): AccountProfile {
  if (!parsed) {
    return emptyProfile
  }

  return {
    username: readString(parsed.preferred_username),
    email: readString(parsed.email),
    name: readString(parsed.name),
    givenName: readString(parsed.given_name),
    familyName: readString(parsed.family_name),
    subject: readString(parsed.sub),
    emailVerified: typeof parsed.email_verified === 'boolean' ? parsed.email_verified : null,
  }
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value : null
}
