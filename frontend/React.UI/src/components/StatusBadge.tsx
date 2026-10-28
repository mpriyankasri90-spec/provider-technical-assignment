import type { LicenseStatus, ProviderStatus } from '../api/types'

const providerTones: Record<ProviderStatus, string> = {
  Active: 'success',
  Inactive: 'neutral',
  Pending: 'info',
}

const licenseTones: Record<LicenseStatus, string> = {
  Active: 'success',
  Expired: 'danger',
  Suspended: 'warning',
}

export function ProviderStatusBadge({ status }: { status: ProviderStatus }) {
  return <span className={`badge badge-${providerTones[status]}`}>{status}</span>
}

export function LicenseStatusBadge({ status }: { status: LicenseStatus }) {
  return <span className={`badge badge-${licenseTones[status]}`}>{status}</span>
}
