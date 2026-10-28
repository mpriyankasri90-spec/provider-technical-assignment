import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { ApiError } from '../../api/client'
import { lookupsApi, providersApi } from '../../api/endpoints'
import type { ProviderDetail, ProviderStatus } from '../../api/types'
import { EmptyState, ErrorAlert, LoadingState } from '../../components/States'
import { toApiError, useApi } from '../../hooks/useApi'

interface FormValues {
  providerName: string
  county: string
  status: ProviderStatus | ''
}

type FieldErrors = Partial<Record<keyof FormValues, string>>

const emptyForm: FormValues = { providerName: '', county: '', status: '' }

// Mirrors the API's rules for quick feedback. The API remains the authority and its
// validation errors are shown against the same fields.
function validate(values: FormValues): FieldErrors {
  const errors: FieldErrors = {}
  if (!values.providerName.trim()) errors.providerName = 'Provider name is required.'
  else if (values.providerName.trim().length > 200) errors.providerName = 'Provider name must be 200 characters or fewer.'
  if (!values.county.trim()) errors.county = 'County is required.'
  else if (values.county.trim().length > 100) errors.county = 'County must be 100 characters or fewer.'
  if (!values.status) errors.status = 'Status is required.'
  return errors
}

/** Create (/providers/new) and edit (/providers/:id/edit) share one form. */
export function ProviderFormPage() {
  const { id } = useParams()
  const isEdit = id !== undefined
  const providerId = Number(id)
  const navigate = useNavigate()

  const lookups = useApi((signal) => lookupsApi.get(signal), [])
  const existing = useApi<ProviderDetail | undefined>(
    (signal) => (isEdit ? providersApi.get(providerId, signal) : Promise.resolve(undefined)),
    [isEdit, providerId],
  )

  const [values, setValues] = useState<FormValues>(emptyForm)
  const [version, setVersion] = useState<number>()
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({})
  const [submitError, setSubmitError] = useState<ApiError>()
  const [submitting, setSubmitting] = useState(false)

  // Fill the form whenever the provider is (re)loaded, including after "Reload latest".
  useEffect(() => {
    if (!existing.data) return
    setValues({ providerName: existing.data.providerName, county: existing.data.county, status: existing.data.status })
    setVersion(existing.data.version)
    setFieldErrors({})
  }, [existing.data])

  function change(field: keyof FormValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }))
    setFieldErrors((current) => ({ ...current, [field]: undefined }))
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSubmitError(undefined)

    const errors = validate(values)
    setFieldErrors(errors)
    if (Object.keys(errors).length > 0) return

    const input = {
      providerName: values.providerName.trim(),
      county: values.county.trim(),
      status: values.status as ProviderStatus,
    }

    setSubmitting(true)
    try {
      const saved = isEdit
        ? await providersApi.update(providerId, { ...input, version: version! })
        : await providersApi.create(input)
      navigate(`/providers/${saved.providerId}`, {
        state: { notice: isEdit ? 'Changes saved.' : 'Provider created.' },
      })
    } catch (caught) {
      const error = toApiError(caught)
      if (error.isValidation) {
        const serverErrors: FieldErrors = {
          providerName: error.fieldErrors.providerName?.[0],
          county: error.fieldErrors.county?.[0],
          status: error.fieldErrors.status?.[0],
        }
        setFieldErrors(serverErrors)
        // Errors that do not belong to a visible field (e.g. malformed request) go to the banner.
        if (!Object.values(serverErrors).some(Boolean)) setSubmitError(error)
      } else if (error.code === 'duplicate_provider') {
        setFieldErrors({ providerName: error.detail })
      } else {
        setSubmitError(error)
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (isEdit && existing.error?.isNotFound) {
    return (
      <EmptyState title="Provider not found">
        <p className="muted">It may have been deleted by another user.</p>
        <Link to="/providers">Back to providers</Link>
      </EmptyState>
    )
  }
  if (existing.error) return <ErrorAlert error={existing.error} onRetry={existing.reload} />
  if (isEdit && !existing.data) return <LoadingState label="Loading provider…" />

  const title = isEdit ? `Edit ${existing.data?.providerName}` : 'Add provider'
  const cancelTo = isEdit ? `/providers/${providerId}` : '/providers'
  const isStale = submitError?.code === 'concurrency_conflict'

  return (
    <section className="form-page">
      <p className="breadcrumb">
        <Link to="/providers">Providers</Link> / {isEdit ? 'Edit' : 'New'}
      </p>
      <h1>{title}</h1>

      {submitError && !isStale && <ErrorAlert error={submitError} />}
      {isStale && (
        <div className="alert alert-warning" role="alert">
          <div>
            <strong>This provider was changed by someone else.</strong>
            <p>Reload the latest version, then re-apply your changes. Your unsaved edits will be replaced.</p>
          </div>
          <button
            type="button"
            className="button"
            onClick={() => {
              setSubmitError(undefined)
              existing.reload()
            }}
          >
            Reload latest
          </button>
        </div>
      )}

      <form className="form" onSubmit={submit} noValidate>
        <Field label="Provider name" error={fieldErrors.providerName} htmlFor="providerName">
          <input
            id="providerName"
            value={values.providerName}
            maxLength={200}
            autoComplete="off"
            aria-invalid={Boolean(fieldErrors.providerName)}
            aria-describedby={fieldErrors.providerName ? 'providerName-error' : undefined}
            onChange={(event) => change('providerName', event.target.value)}
          />
        </Field>

        <Field label="County" error={fieldErrors.county} htmlFor="county">
          <input
            id="county"
            list="county-options"
            value={values.county}
            maxLength={100}
            autoComplete="off"
            aria-invalid={Boolean(fieldErrors.county)}
            aria-describedby={fieldErrors.county ? 'county-error' : undefined}
            onChange={(event) => change('county', event.target.value)}
          />
          <datalist id="county-options">
            {lookups.data?.counties.map((county) => (
              <option key={county} value={county} />
            ))}
          </datalist>
        </Field>

        <Field label="Status" error={fieldErrors.status} htmlFor="status">
          <select
            id="status"
            value={values.status}
            aria-invalid={Boolean(fieldErrors.status)}
            aria-describedby={fieldErrors.status ? 'status-error' : undefined}
            onChange={(event) => change('status', event.target.value)}
          >
            <option value="">Select a status</option>
            {lookups.data?.providerStatuses.map((status) => (
              <option key={status}>{status}</option>
            ))}
          </select>
        </Field>

        <div className="form-actions">
          <button type="submit" className="button button-primary" disabled={submitting || existing.loading}>
            {submitting ? 'Saving…' : isEdit ? 'Save changes' : 'Create provider'}
          </button>
          <Link to={cancelTo} className="button">
            Cancel
          </Link>
        </div>
      </form>
    </section>
  )
}

function Field({
  label,
  error,
  htmlFor,
  children,
}: {
  label: string
  error?: string
  htmlFor: string
  children: ReactNode
}) {
  return (
    <div className="field">
      <label className="field-label" htmlFor={htmlFor}>
        {label}
      </label>
      {children}
      {error && (
        <p className="field-error" id={`${htmlFor}-error`}>
          {error}
        </p>
      )}
    </div>
  )
}
