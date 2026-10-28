import { useEffect, useState } from 'react'
import { ApiError } from '../../api/client'
import { providersApi } from '../../api/endpoints'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { toApiError } from '../../hooks/useApi'

export interface DeletableProvider {
  providerId: number
  providerName: string
  version: number
}

interface DeleteProviderDialogProps {
  provider: DeletableProvider | null
  onCancel: () => void
  onDeleted: (provider: DeletableProvider) => void
}

const maxReasonLength = 500

/** Confirmation step for soft deletion. Sends the loaded version so a stale delete is rejected. */
export function DeleteProviderDialog({ provider, onCancel, onDeleted }: DeleteProviderDialogProps) {
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<ApiError>()

  useEffect(() => {
    setReason('')
    setError(undefined)
  }, [provider?.providerId])

  async function confirm() {
    if (!provider) return
    setBusy(true)
    setError(undefined)
    try {
      await providersApi.softDelete(provider.providerId, {
        reason: reason.trim() || undefined,
        version: provider.version,
      })
      onDeleted(provider)
    } catch (caught) {
      setError(toApiError(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <ConfirmDialog
      open={provider !== null}
      title="Delete provider?"
      confirmLabel="Delete provider"
      tone="danger"
      busy={busy}
      error={error}
      onConfirm={confirm}
      onCancel={onCancel}
    >
      <p>
        <strong>{provider?.providerName}</strong> will be removed from provider listings and reports.
      </p>
      <p className="muted">
        The record and its licenses are kept in the database for audit and can be restored from{' '}
        <em>Deleted providers</em>.
      </p>
      <label className="field">
        <span className="field-label">Reason (optional)</span>
        <textarea
          value={reason}
          maxLength={maxReasonLength}
          rows={3}
          onChange={(event) => setReason(event.target.value)}
          placeholder="e.g. Duplicate record, provider closed"
        />
      </label>
    </ConfirmDialog>
  )
}
