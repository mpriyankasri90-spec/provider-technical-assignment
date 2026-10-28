import { useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import { providersApi } from '../../api/endpoints'
import { LicenseTable } from '../../components/LicenseTable'
import { EmptyState, ErrorAlert, LoadingState, Notice } from '../../components/States'
import { ProviderStatusBadge } from '../../components/StatusBadge'
import { useApi } from '../../hooks/useApi'
import { formatTimestamp, plural } from '../../utils/format'
import { DeleteProviderDialog } from './DeleteProviderDialog'

export function ProviderDetailPage() {
  const providerId = Number(useParams().id)
  const navigate = useNavigate()
  const location = useLocation()
  const [confirmingDelete, setConfirmingDelete] = useState(false)
  const [notice, setNotice] = useState((location.state as { notice?: string } | null)?.notice)

  const { data: provider, error, reload } = useApi(
    (signal) => providersApi.get(providerId, signal),
    [providerId],
  )

  if (error?.isNotFound) {
    return (
      <EmptyState title="Provider not found">
        <p className="muted">It may have been deleted. Deleted providers are listed under Deleted providers.</p>
        <Link to="/providers">Back to providers</Link>
      </EmptyState>
    )
  }

  if (error) return <ErrorAlert error={error} onRetry={reload} />
  // The id check avoids briefly showing the previous provider when navigating between providers.
  if (!provider || provider.providerId !== providerId) return <LoadingState label="Loading provider…" />

  const expiredCount = provider.licenses.filter((license) => license.isExpired).length
  const validCount = provider.licenses.filter((license) => license.isCurrentlyValid).length

  return (
    <section>
      <p className="breadcrumb">
        <Link to="/providers">Providers</Link> / {provider.providerName}
      </p>

      <header className="page-header">
        <div>
          <h1>
            {provider.providerName} <ProviderStatusBadge status={provider.status} />
          </h1>
          <p className="muted">{provider.county} County</p>
        </div>
        <div className="actions">
          <Link to={`/providers/${provider.providerId}/edit`} className="button">
            Edit
          </Link>
          <button type="button" className="button button-danger-outline" onClick={() => setConfirmingDelete(true)}>
            Delete
          </button>
        </div>
      </header>

      {notice && <Notice onDismiss={() => setNotice(undefined)}>{notice}</Notice>}

      {provider.status === 'Active' && expiredCount > 0 && (
        <Notice tone="warning">
          This provider is <strong>Active</strong> but holds {plural(expiredCount, 'expired license')}. Provider
          status does not guarantee license validity.
        </Notice>
      )}

      <dl className="details">
        <div>
          <dt>Created</dt>
          <dd>
            {formatTimestamp(provider.createdAtUtc)} by {provider.createdBy}
          </dd>
        </div>
        <div>
          <dt>Last modified</dt>
          <dd>
            {provider.modifiedAtUtc ? `${formatTimestamp(provider.modifiedAtUtc)} by ${provider.modifiedBy}` : 'Never'}
          </dd>
        </div>
        <div>
          <dt>Record version</dt>
          <dd>{provider.version}</dd>
        </div>
      </dl>

      <h2>
        Licenses <span className="muted heading-note">{validCount} currently valid of {provider.licenses.length}</span>
      </h2>
      {provider.licenses.length === 0 ? (
        <EmptyState title="No licenses on record for this provider." />
      ) : (
        <LicenseTable licenses={provider.licenses} />
      )}

      <DeleteProviderDialog
        provider={confirmingDelete ? provider : null}
        onCancel={() => {
          setConfirmingDelete(false)
          reload()
        }}
        onDeleted={(deleted) =>
          navigate('/providers', {
            state: { notice: `“${deleted.providerName}” was deleted. It can be restored from Deleted providers.` },
          })
        }
      />
    </section>
  )
}
