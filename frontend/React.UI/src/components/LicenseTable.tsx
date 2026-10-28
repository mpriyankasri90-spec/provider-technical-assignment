import type { License } from '../api/types'
import { describeExpiry, formatDate } from '../utils/format'
import { LicenseStatusBadge } from './StatusBadge'

/**
 * Shows both the recorded status and the effective status computed by the API. A license
 * still recorded as Active after its expiration date is flagged for follow-up.
 */
export function LicenseTable({ licenses, caption }: { licenses: License[]; caption?: string }) {
  return (
    <div className="table-wrap">
      <table className="table table-fixed">
        {caption && <caption>{caption}</caption>}
        <thead>
          <tr>
            <th scope="col">License #</th>
            <th scope="col">Recorded status</th>
            <th scope="col">Effective status</th>
            <th scope="col">Issued</th>
            <th scope="col">Expires</th>
            <th scope="col">Time remaining</th>
          </tr>
        </thead>
        <tbody>
          {licenses.map((license) => {
            const staleRecord = license.licenseStatus !== license.effectiveStatus
            return (
              <tr key={license.licenseId} className={license.isExpired ? 'row-danger' : undefined}>
                <td className="mono">{license.licenseNumber}</td>
                <td>
                  <LicenseStatusBadge status={license.licenseStatus} />
                  {staleRecord && <span className="hint">record not updated</span>}
                </td>
                <td>
                  <LicenseStatusBadge status={license.effectiveStatus} />
                </td>
                <td>{formatDate(license.issuedDate)}</td>
                <td>{formatDate(license.expirationDate)}</td>
                <td className={license.daysUntilExpiration < 0 ? 'text-danger' : undefined}>
                  {describeExpiry(license.daysUntilExpiration)}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
