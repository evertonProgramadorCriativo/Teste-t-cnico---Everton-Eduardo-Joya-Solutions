interface PaginationProps {
  page: number;
  totalPages: number;
  totalItems: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

export default function Pagination({ page, totalPages, totalItems, pageSize, onPageChange }: PaginationProps) {
  if (totalItems === 0) return null;

  const firstItem = (page - 1) * pageSize + 1;
  const lastItem = Math.min(page * pageSize, totalItems);

  return (
    <div className="pagination">
      <span className="pagination-summary">Exibindo <strong>{firstItem}–{lastItem}</strong> de <strong>{totalItems}</strong> pedidos</span>
      <div className="pagination-actions">
        <button
          className="button button-secondary button-small"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
        >Anterior</button>
        <span className="page-indicator">Página <strong>{page}</strong> de {Math.max(totalPages, 1)}</span>
        <button
          className="button button-secondary button-small"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
        >Próxima</button>
      </div>
    </div>
  );
}
