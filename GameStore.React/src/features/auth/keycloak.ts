import Keycloak from 'keycloak-js'

const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080',
  realm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'GameStore',
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'gamestore',
})

let initPromise: Promise<boolean> | undefined

export function getKeycloak() {
  return keycloak
}

export function initKeycloak() {
  initPromise ??= keycloak.init({
    onLoad: 'check-sso',
    pkceMethod: 'S256',
    checkLoginIframe: false,
    silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
  })

  return initPromise
}

export function loginToStore() {
  return keycloak.login({
    redirectUri: window.location.origin,
  })
}

export function logoutFromStore() {
  return keycloak.logout({
    redirectUri: window.location.origin,
  })
}

export async function getAccessToken(): Promise<string | null> {
  if (!keycloak.authenticated) {
    return null
  }

  try {
    await keycloak.updateToken(30)
  } catch {
    return null
  }

  return keycloak.token ?? null
}

export function openAccountManagement() {
  const accountUrl = keycloak.createAccountUrl({
    redirectUri: `${window.location.origin}/account`,
  })
  window.location.assign(accountUrl)
}

/** Forces a token refresh so claim changes from Account Console are reflected. */
export async function refreshKeycloakSession(): Promise<boolean> {
  await initKeycloak()

  if (!keycloak.authenticated) {
    return false
  }

  try {
    // Large minValidity forces a refresh so Account Console claim updates are loaded.
    await keycloak.updateToken(Number.MAX_SAFE_INTEGER)
    return true
  } catch {
    return false
  }
}
