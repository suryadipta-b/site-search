import React from 'react';

interface PaginationProps {
  currentPage: number;
  pageSize: number;
  totalHits: number;
  onPageChange: (newPage: number) => void;
}

export const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  pageSize,
  totalHits,
  onPageChange
}) => {
  const totalPages = Math.max(1, Math.ceil(totalHits / pageSize));

  if (totalPages <= 1) return null;

  return (
    <nav className="pagination" aria-label="Search results pagination">
      <button
        type="button"
        className="pagination-btn"
        disabled={currentPage <= 1}
        onClick={() => onPageChange(currentPage - 1)}
      >
        ← Previous
      </button>

      <span className="pagination-info">
        Page <strong>{currentPage}</strong> of <strong>{totalPages}</strong> ({totalHits} results)
      </span>

      <button
        type="button"
        className="pagination-btn"
        disabled={currentPage >= totalPages}
        onClick={() => onPageChange(currentPage + 1)}
      >
        Next →
      </button>
    </nav>
  );
};
