import { getApiBaseUrl } from './config'
import { getAccessToken } from '../features/auth/keycloak'

export class ApiValidationError extends Error {
  readonly fieldErrors: Record<string, string[]>

  constructor(message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiValidationError'
    this.fieldErrors = fieldErrors
  }
}

export async function authorizedFetch(
  path: string,
  init: RequestInit,
  notAuthenticatedMessage: string,
): Promise<Response> {
  const token = await getAccessToken()
  if (!token) {
    throw new Error(notAuthenticatedMessage)
  }

  const headers = new Headers(init.headers)
  headers.set('Authorization', `Bearer ${token}`)

  return fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
  })
}

export async function throwIfNotOk(
  response: Response,
  fallback: string,
  forbiddenMessage = 'You do not have permission to perform this action.',
): Promise<void> {
  if (response.ok) {
    return
  }

  throw await readApiError(response, fallback, forbiddenMessage)
}

async function readApiError(
  response: Response,
  fallback: string,
  forbiddenMessage: string,
): Promise<Error> {
  try {
    const body = (await response.json()) as {
      detail?: unknown
      title?: unknown
      errors?: Record<string, string[] | undefined>
    }

    const fieldErrors: Record<string, string[]> = {}
    if (body.errors && typeof body.errors === 'object') {
      for (const [key, value] of Object.entries(body.errors)) {
        if (Array.isArray(value) && value.length > 0) {
          fieldErrors[key] = value.filter((entry): entry is string => typeof entry === 'string')
        }
      }
    }

    if (Object.keys(fieldErrors).length > 0) {
      const first = Object.values(fieldErrors)[0]?.[0]
      return new ApiValidationError(first ?? 'Please correct the highlighted fields.', fieldErrors)
    }

    if (typeof body.detail === 'string' && body.detail.trim().length > 0) {
      return new Error(body.detail)
    }

    if (typeof body.title === 'string' && body.title.trim().length > 0) {
      return new Error(body.title)
    }
  } catch {
    // Use the fallback when the response is not problem+json.
  }

  if (response.status === 403) {
    return new Error(forbiddenMessage)
  }

  return new Error(`${fallback} (${response.status}).`)
}
