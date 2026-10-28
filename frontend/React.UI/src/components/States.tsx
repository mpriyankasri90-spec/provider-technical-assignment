import type { ReactNode } from 'react'
import type { ApiError } from '../api/client'

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <p className="state state-loading" role="status" aria-live="polite">
      <span className="spinner" aria-hidden="true" />
      {label}
    </p>
  )
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="state state-empty">
      <p className="state-title">{title}</p>
      {children}
    </div>
  )
}

export function ErrorAlert({ error, onRetry }: { error: ApiError; onRetry?: () => void }) {
  return (
    <div className="alert alert-danger" role="alert">
      <div>
        <strong>{error.title}</strong>
        {error.detail && <p>{error.detail}</p>}
      </div>
      {onRetry && (
        <button type="button" className="button" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}

export function Notice({
  tone = 'success',
  children,
  onDismiss,
}: {
  tone?: 'success' | 'warning' | 'info'
  children: ReactNode
  onDismiss?: () => void
}) {
  return (
    <div className={`alert alert-${tone}`} role="status">
      <div>{children}</div>
      {onDismiss && (
        <button type="button" className="button button-ghost" onClick={onDismiss} aria-label="Dismiss message">
          ×
        </button>
      )}
    </div>
  )
}
