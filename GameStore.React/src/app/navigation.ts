export function navigate(path: string) {
  if (window.location.pathname === path) {
    return
  }

  window.history.pushState(null, '', path)
  window.dispatchEvent(new PopStateEvent('popstate'))
}
