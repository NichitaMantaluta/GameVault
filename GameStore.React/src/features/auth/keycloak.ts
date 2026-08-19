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
