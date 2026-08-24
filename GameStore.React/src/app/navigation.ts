export function navigate(path: string) {
  const nextUrl = new URL(path, window.location.origin)
  const next = `${nextUrl.pathname}${nextUrl.search}`
  const current = `${window.location.pathname}${window.location.search}`

  if (current === next) {
    return
  }

  window.history.pushState(null, '', next)
  window.dispatchEvent(new PopStateEvent('popstate'))
}
