// Date-only values ("2028-01-14") are built from their parts so they never shift a day
// across time zones; timestamps are UTC ISO strings shown in the viewer's local time.

export function formatDate(isoDate: string | null | undefined) {
  if (!isoDate) return '—'
  const [year, month, day] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export function formatTimestamp(isoTimestamp: string | null | undefined) {
  if (!isoTimestamp) return '—'
  return new Date(isoTimestamp).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
}

export function describeExpiry(daysUntilExpiration: number) {
  if (daysUntilExpiration < 0) {
    const days = -daysUntilExpiration
    return `expired ${days} day${days === 1 ? '' : 's'} ago`
  }
  if (daysUntilExpiration === 0) return 'expires today'
  return `${daysUntilExpiration} day${daysUntilExpiration === 1 ? '' : 's'} left`
}

export function plural(count: number, noun: string) {
  return `${count} ${noun}${count === 1 ? '' : 's'}`
}
