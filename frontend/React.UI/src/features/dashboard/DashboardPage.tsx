import type { ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router'
import { dashboardApi } from '../../api/endpoints'
import type { DashboardSummary, ProviderStatus } from '../../api/types'
import { BarList } from '../../components/BarList'
import { EmptyState, ErrorAlert, LoadingState } from '../../components/States'
import { ProviderStatusBadge } from '../../components/StatusBadge'
import { useApi } from '../../hooks/useApi'
import { describeExpiry, formatDate, plural } from '../../utils/format'

const windows = [30, 60, 90] as const

// Magnitude across provider statuses: one hue. License states use the reserved status colors,
// always paired with an icon and a text label.
const providerBarColor = '#2a78d6'
const providerStatusOrder: ProviderStatus[] = ['Active', 'Pending', 'Inactive']
const licenseColors = { valid: '#0ca30c', suspended: '#fab219', expired: '#d03b3b' }

/** Read-only overview. All figures come from GET /api/dashboard/summary. */
export function DashboardPage() {
  const [params, setParams] = useSearchParams()
  const requested = Number(params.get('days'))
  const days = windows.find((w) => w === requested) ?? 60

  const { data, error, loading, reload } = useApi((signal) => dashboardApi.summary(days, signal), [days])

  return (
    <section>
      <header className="page-header">
        <div>
          <h1>Dashboard</h1>
          <p className="muted">
            Read-only overview of non-deleted providers.
            {data && ` License validity as of ${formatDate(data.asOfDate)} (UTC).`}
          </p>
        </div>
      </header>

      {error && <ErrorAlert error={error} onRetry={reload} />}
      {!data && loading && <LoadingState label="Loading dashboard…" />}
      {data && (
        <DashboardContent
          summary={data}
          days={days}
          onDaysChange={(next) => setParams({ days: String(next) }, { replace: true })}
        />
      )}
    </section>
  )
}

function DashboardContent({
  summary,
  days,
  onDaysChange,
}: {
  summary: DashboardSummary
  days: number
  onDaysChange: (days: number) => void
}) {
  const { providers, licenses } = summary
  const activeProviders = providers.byStatus.find((s) => s.status === 'Active')?.count ?? 0
  const atRisk = providers.activeWithExpiredLicenses

  return (
    <>
      <div className="kpi-row">
        <div className={`kpi kpi-hero${atRisk > 0 ? ' kpi-alert' : ''}`}>
          <p className="kpi-label">Active providers with expired licenses</p>
          <p className="kpi-hero-value">{atRisk}</p>
          <p className="kpi-note">
            {atRisk > 0 ? (
              <>
                <span aria-hidden="true">⚠</span> Needs follow-up · of {plural(activeProviders, 'active provider')} ·{' '}
                <Link to="/reports?view=expired">View report</Link>
              </>
            ) : (
              'No active provider holds an expired license.'
            )}
          </p>
        </div>
        <Kpi label="Providers" value={providers.total} note={`${activeProviders} active`} />
        <Kpi label="Licenses currently valid" value={licenses.currentlyValid} note={`of ${licenses.total} licenses`} />
        <Kpi label="Licenses expired" value={licenses.expired} note="past expiration date" />
        <Kpi
          label="Deleted providers"
          value={providers.deleted}
          note={<Link to="/audit/deleted-providers">Audit view</Link>}
        />
      </div>

      <div className="chart-grid">
        <BarList
          title="Providers by status"
          description="Provider lifecycle status. Status alone does not imply license validity."
          noun="provider"
          rows={providerStatusOrder.map((status) => ({
            label: status,
            value: providers.byStatus.find((s) => s.status === status)?.count ?? 0,
            color: providerBarColor,
          }))}
        />
        <BarList
          title="Licenses by effective status"
          description="Expired is decided by the expiration date, even when the recorded status still says Active."
          noun="license"
          rows={[
            { label: 'Currently valid', value: licenses.currentlyValid, color: licenseColors.valid, icon: '✓' },
            { label: 'Suspended', value: licenses.suspended, color: licenseColors.suspended, icon: '‖' },
            { label: 'Expired', value: licenses.expired, color: licenseColors.expired, icon: '✕' },
          ]}
        />
      </div>

      <div className="section-header">
        <h2>Licenses expiring soon</h2>
        <label className="field field-inline">
          <span className="field-label">Window</span>
          <select value={days} onChange={(event) => onDaysChange(Number(event.target.value))}>
            {windows.map((w) => (
              <option key={w} value={w}>
                Next {w} days
              </option>
            ))}
          </select>
        </label>
      </div>

      {summary.expiringLicenses.length === 0 ? (
        <EmptyState title={`No valid licenses expire in the next ${days} days.`} />
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th scope="col">Provider</th>
                <th scope="col">Provider status</th>
                <th scope="col">License #</th>
                <th scope="col">Expires</th>
                <th scope="col">Time remaining</th>
              </tr>
            </thead>
            <tbody>
              {summary.expiringLicenses.map((license) => (
                <tr key={license.licenseId}>
                  <td>
                    <Link to={`/providers/${license.providerId}`}>{license.providerName}</Link>
                  </td>
                  <td>
                    <ProviderStatusBadge status={license.providerStatus} />
                  </td>
                  <td className="mono">{license.licenseNumber}</td>
                  <td>{formatDate(license.expirationDate)}</td>
                  <td>{describeExpiry(license.daysUntilExpiration)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}

function Kpi({ label, value, note }: { label: string; value: number; note: ReactNode }) {
  return (
    <div className="kpi">
      <p className="kpi-label">{label}</p>
      <p className="kpi-value">{value.toLocaleString()}</p>
      <p className="kpi-note">{note}</p>
    </div>
  )
}
