import { useCallback, useEffect, useState, type DependencyList } from 'react'
import { ApiError, isAbortError } from '../api/client'

interface ApiState<T> {
  data?: T
  error?: ApiError
  loading: boolean
}

/**
 * Loads data when `deps` change and exposes loading/error state plus `reload()`.
 * Previous data stays visible while reloading (no flicker when filters change), and a
 * superseded request is aborted so a slow response cannot overwrite a newer one.
 */
export function useApi<T>(load: (signal: AbortSignal) => Promise<T>, deps: DependencyList) {
  const [state, setState] = useState<ApiState<T>>({ loading: true })
  const [reloadCount, setReloadCount] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setState((previous) => ({ data: previous.data, loading: true }))

    load(controller.signal).then(
      (data) => {
        if (!controller.signal.aborted) setState({ data, loading: false })
      },
      (error: unknown) => {
        if (controller.signal.aborted || isAbortError(error)) return
        setState({ error: toApiError(error), loading: false })
      },
    )

    return () => controller.abort()
    // `load` is recreated on every render; `deps` lists what it actually depends on.
  }, [...deps, reloadCount])

  const reload = useCallback(() => setReloadCount((count) => count + 1), [])

  return { ...state, reload }
}

export function toApiError(error: unknown): ApiError {
  return error instanceof ApiError ? error : new ApiError(0, 'Unexpected error', String(error))
}
