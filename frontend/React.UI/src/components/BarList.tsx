export interface BarListRow {
  label: string
  value: number
  color: string
  /** Optional glyph shown with the label, so state is never conveyed by color alone. */
  icon?: string
}

interface BarListProps {
  title: string
  description: string
  rows: BarListRow[]
  noun: string
}

/**
 * Labelled horizontal bars for comparing a few counts. Rendered as a table, so the label and
 * value of every bar are readable text (and the table is the accessible data view); the bar
 * only adds magnitude. Hover or focus a row for its share of the total.
 */
export function BarList({ title, description, rows, noun }: BarListProps) {
  const max = Math.max(...rows.map((row) => row.value), 1)
  const total = rows.reduce((sum, row) => sum + row.value, 0)

  return (
    <figure className="chart">
      <figcaption>
        <h2 className="chart-title">{title}</h2>
        <p className="muted chart-description">{description}</p>
      </figcaption>
      <table className="bar-list">
        <thead className="visually-hidden">
          <tr>
            <th scope="col">Category</th>
            <th scope="col">Bar</th>
            <th scope="col">Count</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => {
            const share = total === 0 ? 0 : Math.round((row.value / total) * 100)
            return (
              <tr key={row.label} className="bar-row" tabIndex={0}>
                <th scope="row" className="bar-label">
                  {row.icon && (
                    <span className="bar-icon" aria-hidden="true">
                      {row.icon}
                    </span>
                  )}
                  {row.label}
                </th>
                <td className="bar-cell" aria-hidden="true">
                  <div className="bar-track">
                    <div className="bar" style={{ width: `${(row.value / max) * 100}%`, background: row.color }} />
                  </div>
                  <span className="bar-tooltip" role="tooltip">
                    {row.label}: {row.value} {noun}
                    {row.value === 1 ? '' : 's'} · {share}% of {total}
                  </span>
                </td>
                <td className="bar-value">{row.value}</td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </figure>
  )
}
