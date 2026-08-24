const FEEDBACK_KEY = 'account.catalog.feedback'

export function setCatalogFeedback(message: string) {
  sessionStorage.setItem(FEEDBACK_KEY, message)
}

export function consumeCatalogFeedback(): string | null {
  const message = sessionStorage.getItem(FEEDBACK_KEY)
  if (!message) {
    return null
  }

  sessionStorage.removeItem(FEEDBACK_KEY)
  return message
}
