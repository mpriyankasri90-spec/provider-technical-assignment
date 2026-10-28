import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router'
import { lookupsApi, providersApi } from '../../api/endpoints'
import type { ProviderStatus } from '../../api/types'
import { Pagination } from '../../components/Pagination'
import { EmptyState, ErrorAlert, LoadingState, Notice } from '../../components/States'
import { ProviderStatusBadge } from '../../components/StatusBadge'
import { useApi } from '../../hooks/useApi'
import { DeleteProviderDialog, type DeletableProvider } from './DeleteProviderDialog'

const pageSize = 10

/** Non-deleted providers. Filters live in the URL so a filtered view can be bookmarked or shared. */
export function ProviderListPage() {
  const [params, setParams] = useSearchParams()
  const location = useLocation()
  const navigate = useNavigate()

  const search = params.get('search') ?? ''
  const status = (params.get('status') ?? '') as ProviderStatus | ''
  const county = params.get('county') ?? ''
  const page = Math.max(1, Number(params.get('page')) || 1)
  const hasFilters = Boolean(search || status || county)

  const [searchInput, setSearchInput] = useState(search)
  const [toDelete, setToDelete] = useState<DeletableProvider | null>(null)
  const [notice, setNotice] = useState<string | undefined>((location.state as { notice?: string } | null)?.notice)

  const lookups = useApi((signal) => lookupsApi.get(signal), [])
  const providers = useApi(
    (signal) => providersApi.list({ search, status: status || undefined, county, page, pageSize }, signal),
    [search, status, county, page],
  )

  // A notice passed by another page (e.g. after a delete) is shown once, not again on refresh.
  useEffect(() => {
    if (location.state) navigate({ search: location.search }, { replace: true, state: null })
  }, [location.state, location.search, navigate])

  // Debounce typing in the search box before it changes the URL (and triggers a request).
  useEffect(() => {
    const trimmed = searchInput.trim()
    if (trimmed === search) return
    const timer = setTimeout(() => updateParams({ search: trimmed }), 300)
    return () => clearTimeout(timer)
    // updateParams only depends on the current params, which `search` reflects.
  }, [searchInput, search])

  function updateParams(changes: Record<string, string | number>) {
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        for (const [key, value] of Object.entries(changes)) {
          if (value === '' || (key === 'page' && value === 1)) next.delete(key)
          else next.set(key, String(value))
        }
        if (!('page' in changes)) next.delete('page')
        return next
      },
      { replace: true },
    )
  }

  function clearFilters() {
    setSearchInput('')
    setParams({}, { replace: true })
  }

  const result = providers.data

  return (
    <section>
      <header className="page-header">
        <div>
          <h1>Providers</h1>
          <p className="muted">Deleted providers are excluded from this list.</p>
        </div>
        <Link to="/providers/new" className="button button-primary">
          Add provider
        </Link>
      </header>

      {notice && <Notice onDismiss={() => setNotice(undefined)}>{notice}</Notice>}

      <form className="filters" role="search" onSubmit={(event) => event.preventDefault()}>
        <label className="field">
          <span className="field-label">Search by name</span>
          <input type="search" value={searchInput} maxLength={200} onChange={(event) => setSearchInput(event.target.value)} />
        </label>
        <label className="field">
          <span className="field-label">Status</span>
          <select value={status} onChange={(event) => updateParams({ status: event.target.value })}>
            <option value="">All statuses</option>
            {lookups.data?.providerStatuses.map((value) => (
              <option key={value}>{value}</option>
            ))}
          </select>
        </label>
        <label className="field">
          <span className="field-label">County</span>
          <select value={county} onChange={(event) => updateParams({ county: event.target.value })}>
            <option value="">All counties</option>
            {lookups.data?.counties.map((value) => (
              <option key={value}>{value}</option>
            ))}
          </select>
        </label>
        {hasFilters && (
          <button type="button" className="button button-ghost" onClick={clearFilters}>
            Clear filters
          </button>
        )}
      </form>

      {providers.error && <ErrorAlert error={providers.error} onRetry={providers.reload} />}

      {!result && providers.loading && <LoadingState label="Loading providers…" />}

      {result && result.items.length === 0 && (
        <EmptyState title={hasFilters ? 'No providers match these filters.' : 'No providers yet.'}>
          {hasFilters ? (
            <button type="button" className="button" onClick={clearFilters}>
              Clear filters
            </button>
          ) : (
            <Link to="/providers/new">Add the first provider</Link>
          )}
        </EmptyState>
      )}

      {result && result.items.length > 0 && (
        <>
          <div className="table-wrap" aria-busy={providers.loading}>
            <table className="table">
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col">County</th>
                  <th scope="col">Status</th>
                  <th scope="col">Licenses</th>
                  <th scope="col">
                    <span className="visually-hidden">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((provider) => (
                  <tr key={provider.providerId}>
                    <td>
                      <Link to={`/providers/${provider.providerId}`}>{provider.providerName}</Link>
                    </td>
                    <td>{provider.county}</td>
                    <td>
                      <ProviderStatusBadge status={provider.status} />
                    </td>
                    <td>
                      {provider.licenseCount === 0 ? (
                        <span className="muted">None</span>
                      ) : (
                        <span className="license-summary">
                          {provider.validLicenseCount} valid of {provider.licenseCount}
                          {provider.expiredLicenseCount > 0 && (
                            <span className="badge badge-danger">{provider.expiredLicenseCount} expired</span>
                          )}
                        </span>
                      )}
                    </td>
                    <td>
                      <div className="actions">
                        <Link to={`/providers/${provider.providerId}/edit`} className="button button-small">
                          Edit
                        </Link>
                        <button type="button" className="button button-small button-danger-outline" onClick={() => setToDelete(provider)}>
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            page={result.page}
            totalPages={result.totalPages}
            totalCount={result.totalCount}
            noun="provider"
            onPageChange={(next) => updateParams({ page: next })}
          />
        </>
      )}

      <DeleteProviderDialog
        provider={toDelete}
        onCancel={() => {
          setToDelete(null)
          // A failed delete (e.g. the record changed meanwhile) means this list may be stale.
          providers.reload()
        }}
        onDeleted={(deleted) => {
          setToDelete(null)
          setNotice(`“${deleted.providerName}” was deleted. It can be restored from Deleted providers.`)
          providers.reload()
        }}
      />
    </section>
  )
}
