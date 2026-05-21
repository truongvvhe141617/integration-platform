import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Skeleton } from '../atoms/Skeleton';
import { EmptyState } from '../molecules/EmptyState';

// ── Types ──

export interface Column<T> {
  key: string;
  header: string;
  width?: string;
  render: (item: T, index: number) => React.ReactNode;
  hideOnMobile?: boolean;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  data: T[];
  loading?: boolean;
  emptyIcon?: React.ReactNode;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyAction?: { label: string; onClick: () => void };
  onRowClick?: (item: T) => void;
  pagination?: {
    page: number;
    pageSize: number;
    total: number;
    onPageChange: (page: number) => void;
  };
}

export function DataTable<T>({
  columns, data, loading, emptyIcon, emptyTitle, emptyDescription, emptyAction,
  onRowClick, pagination,
}: DataTableProps<T>) {
  const gridTemplate = columns.map((c) => c.width ?? '1fr').join(' ');

  if (loading) {
    return (
      <div className="table-container">
        <div className="table-header" style={{ gridTemplateColumns: gridTemplate }}>
          {columns.map((col) => (
            <span key={col.key}>{col.header}</span>
          ))}
        </div>
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="table-row" style={{ gridTemplateColumns: gridTemplate, cursor: 'default' }}>
            {columns.map((col) => (
              <div key={col.key}>
                <Skeleton width={60 + Math.random() * 80} height={14} />
              </div>
            ))}
          </div>
        ))}
      </div>
    );
  }

  if (data.length === 0) {
    return (
      <div className="table-container">
        <EmptyState
          icon={emptyIcon}
          title={emptyTitle ?? 'No data'}
          description={emptyDescription}
          action={emptyAction}
        />
      </div>
    );
  }

  return (
    <div className="table-container">
      {/* Header */}
      <div className="table-header" style={{ gridTemplateColumns: gridTemplate }}>
        {columns.map((col) => (
          <span key={col.key} className={col.hideOnMobile ? 'hide-mobile' : ''}>
            {col.header}
          </span>
        ))}
      </div>

      {/* Rows */}
      {data.map((item, index) => (
        <div
          key={index}
          className="table-row"
          style={{ gridTemplateColumns: gridTemplate }}
          onClick={() => onRowClick?.(item)}
        >
          {columns.map((col) => (
            <div key={col.key} className={col.hideOnMobile ? 'hide-mobile' : ''}>
              {col.render(item, index)}
            </div>
          ))}
        </div>
      ))}

      {/* Pagination */}
      {pagination && pagination.total > pagination.pageSize && (
        <div className="pagination">
          <span>
            {(pagination.page - 1) * pagination.pageSize + 1}–
            {Math.min(pagination.page * pagination.pageSize, pagination.total)} of {pagination.total}
          </span>
          <div style={{ display: 'flex', gap: 4 }}>
            <button
              className="pagination-btn"
              disabled={pagination.page <= 1}
              onClick={() => pagination.onPageChange(pagination.page - 1)}
            >
              <ChevronLeft size={14} />
            </button>
            <button
              className="pagination-btn"
              disabled={pagination.page * pagination.pageSize >= pagination.total}
              onClick={() => pagination.onPageChange(pagination.page + 1)}
            >
              <ChevronRight size={14} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
