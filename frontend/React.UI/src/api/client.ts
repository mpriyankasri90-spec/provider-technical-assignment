// Thin fetch wrapper: JSON in/out, and every failure becomes an ApiError built from the
// API's RFC 7807 problem response, so components handle one error shape.

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail?: string
  /** Stable machine-readable reason from the API, e.g. "concurrency_conflict" or "duplicate_provider". */
  readonly code?: string
  /** Field-level validation messages, keyed by camelCase field name (e.g. "providerName"). */
  readonly fieldErrors: Record<string, string[]>

  constructor(
    status: number,
    title: string,
    detail?: string,
    options: { code?: string; fieldErrors?: Record<string, string[]> } = {},
  ) {
    super(detail ?? title)
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
    this.code = options.code
    this.fieldErrors = options.fieldErrors ?? {}
  }

  get isNotFound() {
    return this.status === 404
  }

  get isConflict() {
    return this.status === 409
  }

  get isValidation() {
    return this.status === 400
  }
}

interface ProblemDetails {
  title?: string
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

type RequestOptions = Omit<RequestInit, 'body'> & { json?: unknown }

export async function request<T>(path: string, { json, headers, ...init }: RequestOptions = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(baseUrl + path, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...(json === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...headers,
      },
      body: json === undefined ? undefined : JSON.stringify(json),
    })
  } catch (error) {
    if (isAbortError(error)) throw error
    throw new ApiError(0, 'Cannot reach the server', 'Check that the API is running, then try again.')
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return (response.status === 204 ? undefined : await response.json()) as T
}

export function isAbortError(error: unknown) {
  return error instanceof DOMException && error.name === 'AbortError'
}

export function toQueryString(params: Record<string, string | number | undefined | null>) {
  const query = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') query.set(key, String(value))
  }
  const text = query.toString()
  return text ? `?${text}` : ''
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {}
  try {
    problem = await response.json()
  } catch {
    // Not a problem+json body (e.g. the dev proxy could not reach the API).
  }

  const serverError = response.status >= 500
  return new ApiError(
    response.status,
    problem.title ?? (serverError ? 'The server could not complete the request' : 'Request failed'),
    problem.detail ?? (serverError ? 'The API may be unavailable. Try again in a moment.' : undefined),
    { code: problem.code, fieldErrors: normalizeFieldErrors(problem.errors) },
  )
}

// ASP.NET Core keys errors by C# property ("ProviderName") or JSON path ("$.status").
function normalizeFieldErrors(errors: Record<string, string[]> = {}) {
  const result: Record<string, string[]> = {}
  for (const [key, messages] of Object.entries(errors)) {
    const name = key.replace(/^\$\.?/, '')
    const field = name ? name.charAt(0).toLowerCase() + name.slice(1) : 'form'
    result[field] = [...(result[field] ?? []), ...messages]
  }
  return result
}
