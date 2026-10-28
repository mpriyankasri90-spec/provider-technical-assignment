// Mirrors the C# contracts in backend/Candidate.API/Contracts. Enums travel as names.

export type ProviderStatus = 'Active' | 'Inactive' | 'Pending'
export type LicenseStatus = 'Active' | 'Expired' | 'Suspended'

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ProviderListQuery {
  search?: string
  status?: ProviderStatus
  county?: string
  page?: number
  pageSize?: number
}

export interface ProviderListItem {
  providerId: number
  providerName: string
  county: string
  status: ProviderStatus
  createdAtUtc: string
  version: number
  licenseCount: number
  validLicenseCount: number
  expiredLicenseCount: number
}

/** License validity (isExpired, isCurrentlyValid, effectiveStatus) is computed by the API. */
export interface License {
  licenseId: number
  providerId: number
  licenseNumber: string
  licenseStatus: LicenseStatus
  effectiveStatus: LicenseStatus
  issuedDate: string | null
  expirationDate: string
  isExpired: boolean
  isCurrentlyValid: boolean
  daysUntilExpiration: number
}

export interface ProviderDetail {
  providerId: number
  providerName: string
  county: string
  status: ProviderStatus
  createdAtUtc: string
  createdBy: string
  modifiedAtUtc: string | null
  modifiedBy: string | null
  version: number
  licenses: License[]
}

export interface ProviderInput {
  providerName: string
  county: string
  status: ProviderStatus
}

export interface ProviderUpdate extends ProviderInput {
  version: number
}

export interface ProviderLicenses {
  providerId: number
  providerName: string
  county: string
  status: ProviderStatus
  licenses: License[]
}

export interface DeletedProvider {
  providerId: number
  providerName: string
  county: string
  status: ProviderStatus
  createdAtUtc: string
  createdBy: string
  deletedAtUtc: string | null
  deletedBy: string | null
  deleteReason: string | null
  licenseCount: number
}

export interface Lookups {
  providerStatuses: ProviderStatus[]
  licenseStatuses: LicenseStatus[]
  counties: string[]
}
