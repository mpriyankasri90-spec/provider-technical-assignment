import { Link, useSearchParams } from 'react-router'
import { reportsApi } from '../../api/endpoints'
import type { ProviderLicenses } from '../../api/types'
import { LicenseTable } from '../../components/LicenseTable'
import { EmptyState, ErrorAlert, LoadingState } from '../../components/States'
import { ProviderStatusBadge } from '../../components/StatusBadge'
import { useApi } from '../../hooks/useApi'
import { plural } from '../../utils/format'

const reports = {
  active: {
    label: 'Active providers · valid licenses',
    description:
      'Providers with status Active and their licenses that are currently valid (recorded as Active and not past the expiration date).',
    empty: 'No active provider currently holds a valid license.',
    load: reportsApi.activeProvidersWithActiveLicenses,
  },
  expired: {
    label: 'Active providers · expired licenses',
    description:
      'Providers that appear Active but hold licenses past their expiration date — including licenses whose recorded status was never updated.',
    empty: 'No active provider holds an expired license.',
    load: reportsApi.activeProvidersWithExpiredLicenses,
  },
} as const

type ReportKey = keyof typeof reports

/** The two required license scenarios. Rules are applied by the API; soft-deleted providers never appear. */
export function LicenseReportsPage() {
  const [params, setParams] = useSearchParams()
  const selected: ReportKey = params.get('view') === 'expired' ? 'expired' : 'active'
  const report = reports[selected]

  const { data, error, loading, reload } = useApi((signal) => report.load(signal), [selected])
  const current = loading ? undefined : data

  return (
    <section>
      <header className="page-header">
        <div>
          <h1>License reports</h1>
          <p className="muted">Soft-deleted providers are excluded from every report.</p>
        </div>
      </header>

      <div className="tabs" role="tablist" aria-label="Report">
        {(Object.keys(reports) as ReportKey[]).map((key) => (
          <button
            key={key}
            type="button"
            role="tab"
            id={`tab-${key}`}
            aria-selected={key === selected}
            aria-controls="report-panel"
            className={`tab${key === selected ? ' tab-selected' : ''}`}
            onClick={() => setParams(key === 'active' ? {} : { view: key }, { replace: true })}
          >
            {reports[key].label}
          </button>
        ))}
      </div>

      <div id="report-panel" role="tabpanel" aria-labelledby={`tab-${selected}`}>
        <p className="muted">{report.description}</p>

        {error && <ErrorAlert error={error} onRetry={reload} />}
        {!error && !current && <LoadingState label="Loading report…" />}
        {current && current.length === 0 && <EmptyState title={report.empty} />}
        {current && current.length > 0 && <ReportResults providers={current} />}
      </div>
    </section>
  )
}

function ReportResults({ providers }: { providers: ProviderLicenses[] }) {
  const licenseCount = providers.reduce((total, provider) => total + provider.licenses.length, 0)

  return (
    <>
      <p className="summary">
        {plural(providers.length, 'provider')} · {plural(licenseCount, 'license')}
      </p>
      {providers.map((provider) => (
        <article key={provider.providerId} className="card">
          <h2 className="card-title">
            <Link to={`/providers/${provider.providerId}`}>{provider.providerName}</Link>{' '}
            <ProviderStatusBadge status={provider.status} />
            <span className="muted heading-note">{provider.county} County</span>
          </h2>
          <LicenseTable licenses={provider.licenses} />
        </article>
      ))}
    </>
  )
}
