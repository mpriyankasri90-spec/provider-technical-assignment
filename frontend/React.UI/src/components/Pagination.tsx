import { plural } from '../utils/format'

interface PaginationProps {
  page: number
  totalPages: number
  totalCount: number
  noun: string
  onPageChange: (page: number) => void
}

export function Pagination({ page, totalPages, totalCount, noun, onPageChange }: PaginationProps) {
  return (
    <nav className="pagination" aria-label="Pagination">
      <span>
        {plural(totalCount, noun)}
        {totalPages > 1 && ` · page ${page} of ${totalPages}`}
      </span>
      {totalPages > 1 && (
        <span className="pagination-buttons">
          <button type="button" className="button" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
            Previous
          </button>
          <button type="button" className="button" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
            Next
          </button>
        </span>
      )}
    </nav>
  )
}
