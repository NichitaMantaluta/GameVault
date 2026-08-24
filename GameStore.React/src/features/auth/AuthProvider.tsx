import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import {
  getKeycloak,
  initKeycloak,
  loginToStore,
  logoutFromStore,
  openAccountManagement,
  refreshKeycloakSession,
} from './keycloak'

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
  isAdmin: boolean
  username: string | null
  profile: AccountProfile
  login: () => void
  logout: () => void
  manageAccount: () => void
  refreshProfile: () => Promise<void>
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
  const [isAdmin, setIsAdmin] = useState(false)
  const [profile, setProfile] = useState<AccountProfile>(emptyProfile)

  const applySession = useCallback((authenticated: boolean) => {
    setIsAuthenticated(authenticated)
    setIsAdmin(authenticated ? readIsAdmin(getKeycloak().tokenParsed) : false)
    setProfile(authenticated ? readProfile(getKeycloak().tokenParsed) : emptyProfile)
  }, [])

  useEffect(() => {
    let cancelled = false

    void initKeycloak()
      .then((authenticated) => {
        if (cancelled) {
          return
        }

        applySession(authenticated)
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
  }, [applySession])

  const refreshProfile = useCallback(async () => {
    const authenticated = await refreshKeycloakSession()
    applySession(authenticated)
  }, [applySession])

  const value = useMemo<AuthContextValue>(
    () => ({
      isReady,
      isAuthenticated,
      isAdmin,
      username: profile.username,
      profile,
      login: () => {
        void loginToStore()
      },
      logout: () => {
        void logoutFromStore()
      },
      manageAccount: () => {
        openAccountManagement()
      },
      refreshProfile,
    }),
    [isAdmin, isAuthenticated, isReady, profile, refreshProfile],
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

function readIsAdmin(parsed: unknown): boolean {
  const keycloak = getKeycloak()
  if (keycloak.hasRealmRole?.('Admin')) {
    return true
  }

  if (!parsed || typeof parsed !== 'object') {
    return false
  }

  const claims = parsed as Record<string, unknown>
  if (hasRoleValue(claims.role, 'Admin')) {
    return true
  }

  const realmAccess = claims.realm_access
  if (realmAccess && typeof realmAccess === 'object') {
    const roles = (realmAccess as { roles?: unknown }).roles
    if (hasRoleValue(roles, 'Admin')) {
      return true
    }
  }

  return false
}

function hasRoleValue(value: unknown, role: string): boolean {
  if (typeof value === 'string') {
    return value === role
  }

  if (Array.isArray(value)) {
    return value.some((entry) => entry === role)
  }

  return false
}

function readProfile(parsed: unknown): AccountProfile {
  if (!parsed || typeof parsed !== 'object') {
    return emptyProfile
  }

  const claims = parsed as Record<string, unknown>

  return {
    username: readString(claims.preferred_username),
    email: readString(claims.email),
    name: readString(claims.name),
    givenName: readString(claims.given_name),
    familyName: readString(claims.family_name),
    subject: readString(claims.sub),
    emailVerified: typeof claims.email_verified === 'boolean' ? claims.email_verified : null,
  }
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value : null
}
