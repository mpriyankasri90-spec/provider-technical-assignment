import { useState } from 'react'
import { Link } from 'react-router'
import { ApiError } from '../../api/client'
import { auditApi } from '../../api/endpoints'
import type { DeletedProvider } from '../../api/types'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { EmptyState, ErrorAlert, LoadingState, Notice } from '../../components/States'
import { ProviderStatusBadge } from '../../components/StatusBadge'
import { toApiError, useApi } from '../../hooks/useApi'
import { formatTimestamp, plural } from '../../utils/format'

/**
 * Audit view of soft-deleted providers: the records remain in the database with who deleted
 * them, when and why. In production this page would be limited to administrators.
 */
export function DeletedProvidersPage() {
  const { data, error, loading, reload } = useApi((signal) => auditApi.deletedProviders(signal), [])
  const [toRestore, setToRestore] = useState<DeletedProvider | null>(null)
  const [restoring, setRestoring] = useState(false)
  const [restoreError, setRestoreError] = useState<ApiError>()
  const [restored, setRestored] = useState<DeletedProvider>()

  async function restore() {
    if (!toRestore) return
    setRestoring(true)
    setRestoreError(undefined)
    try {
      await auditApi.restore(toRestore.providerId)
      setRestored(toRestore)
      setToRestore(null)
      reload()
    } catch (caught) {
      setRestoreError(toApiError(caught))
    } finally {
      setRestoring(false)
    }
  }

  return (
    <section>
      <header className="page-header">
        <div>
          <h1>Deleted providers</h1>
          <p className="muted">
            Audit view. Providers are never physically deleted; these records are hidden from standard views.
          </p>
        </div>
      </header>

      {restored && (
        <Notice onDismiss={() => setRestored(undefined)}>
          “{restored.providerName}” was restored.{' '}
          <Link to={`/providers/${restored.providerId}`}>View provider</Link>
        </Notice>
      )}

      {error && <ErrorAlert error={error} onRetry={reload} />}
      {!data && loading && <LoadingState label="Loading deleted providers…" />}
      {data && data.length === 0 && <EmptyState title="No providers have been deleted." />}

      {data && data.length > 0 && (
        <div className="table-wrap" aria-busy={loading}>
          <table className="table">
            <thead>
              <tr>
                <th scope="col">Name</th>
                <th scope="col">County</th>
                <th scope="col">Status when deleted</th>
                <th scope="col">Deleted</th>
                <th scope="col">Reason</th>
                <th scope="col">Licenses retained</th>
                <th scope="col">
                  <span className="visually-hidden">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {data.map((provider) => (
                <tr key={provider.providerId}>
                  <td>{provider.providerName}</td>
                  <td>{provider.county}</td>
                  <td>
                    <ProviderStatusBadge status={provider.status} />
                  </td>
                  <td>
                    {formatTimestamp(provider.deletedAtUtc)}
                    <span className="hint">by {provider.deletedBy ?? 'unknown'}</span>
                  </td>
                  <td>{provider.deleteReason ?? <span className="muted">Not given</span>}</td>
                  <td>{provider.licenseCount}</td>
                  <td>
                    <div className="actions">
                      <button
                        type="button"
                        className="button button-small"
                        onClick={() => {
                          setRestoreError(undefined)
                          setToRestore(provider)
                        }}
                      >
                        Restore
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <ConfirmDialog
        open={toRestore !== null}
        title="Restore provider?"
        confirmLabel="Restore provider"
        busy={restoring}
        error={restoreError}
        onConfirm={restore}
        onCancel={() => setToRestore(null)}
      >
        <p>
          <strong>{toRestore?.providerName}</strong> will reappear in provider listings and reports, together with{' '}
          {plural(toRestore?.licenseCount ?? 0, 'license')}.
        </p>
      </ConfirmDialog>
    </section>
  )
}
