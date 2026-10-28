import { request, toQueryString } from './client'
import type {
  DeletedProvider,
  Lookups,
  PagedResult,
  ProviderDetail,
  ProviderInput,
  ProviderLicenses,
  ProviderListItem,
  ProviderListQuery,
  ProviderUpdate,
} from './types'

export const providersApi = {
  list: (query: ProviderListQuery, signal?: AbortSignal) =>
    request<PagedResult<ProviderListItem>>(`/api/providers${toQueryString({ ...query })}`, { signal }),

  get: (id: number, signal?: AbortSignal) => request<ProviderDetail>(`/api/providers/${id}`, { signal }),

  create: (input: ProviderInput) => request<ProviderDetail>('/api/providers', { method: 'POST', json: input }),

  update: (id: number, input: ProviderUpdate) =>
    request<ProviderDetail>(`/api/providers/${id}`, { method: 'PUT', json: input }),

  /** Soft delete. Passing the loaded version makes the API reject the delete if the record changed. */
  softDelete: (id: number, options: { reason?: string; version?: number }) =>
    request<void>(`/api/providers/${id}${toQueryString(options)}`, { method: 'DELETE' }),
}

export const reportsApi = {
  activeProvidersWithActiveLicenses: (signal?: AbortSignal) =>
    request<ProviderLicenses[]>('/api/reports/active-providers-active-licenses', { signal }),

  activeProvidersWithExpiredLicenses: (signal?: AbortSignal) =>
    request<ProviderLicenses[]>('/api/reports/active-providers-expired-licenses', { signal }),
}

export const auditApi = {
  deletedProviders: (signal?: AbortSignal) => request<DeletedProvider[]>('/api/audit/providers/deleted', { signal }),

  restore: (id: number) => request<void>(`/api/audit/providers/${id}/restore`, { method: 'POST' }),
}

export const lookupsApi = {
  get: (signal?: AbortSignal) => request<Lookups>('/api/lookups', { signal }),
}
