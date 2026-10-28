import { useEffect, useId, useRef, type ReactNode } from 'react'
import type { ApiError } from '../api/client'
import { ErrorAlert } from './States'

interface ConfirmDialogProps {
  open: boolean
  title: string
  confirmLabel: string
  tone?: 'danger' | 'primary'
  busy?: boolean
  error?: ApiError
  onConfirm: () => void
  onCancel: () => void
  children: ReactNode
}

/** Modal confirmation built on the native <dialog> element (focus trapping and Esc for free). */
export function ConfirmDialog({
  open,
  title,
  confirmLabel,
  tone = 'primary',
  busy = false,
  error,
  onConfirm,
  onCancel,
  children,
}: ConfirmDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const titleId = useId()

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  return (
    <dialog
      ref={dialogRef}
      className="dialog"
      aria-labelledby={titleId}
      onCancel={(event) => {
        event.preventDefault()
        if (!busy) onCancel()
      }}
    >
      <form
        onSubmit={(event) => {
          event.preventDefault()
          onConfirm()
        }}
      >
        <h2 id={titleId}>{title}</h2>
        <div className="dialog-body">{children}</div>
        {error && <ErrorAlert error={error} />}
        <div className="dialog-actions">
          <button type="button" className="button" onClick={onCancel} disabled={busy}>
            Cancel
          </button>
          <button type="submit" className={`button button-${tone}`} disabled={busy}>
            {busy ? 'Working…' : confirmLabel}
          </button>
        </div>
      </form>
    </dialog>
  )
}
