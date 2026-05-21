import React from 'react';

export const Skeleton: React.FC<{ style?: React.CSSProperties; className?: string }> = ({ style, className }) => (
  <div
    className={className}
    style={{
      background: 'linear-gradient(90deg, var(--color-bg-subtle) 25%, var(--color-bg-hover) 50%, var(--color-bg-subtle) 75%)',
      backgroundSize: '200% 100%',
      borderRadius: 4,
      animation: 'shimmer 1.5s ease-in-out infinite',
      ...style,
    }}
  />
);

export const TableSkeleton: React.FC<{ rows?: number }> = ({ rows = 5 }) => (
  <div className="table-container" style={{ overflow: 'hidden' }}>
    {/* Header skeleton */}
    <div style={{
      display: 'flex', gap: 16, alignItems: 'center',
      padding: '12px 20px', background: 'var(--color-bg-subtle)',
      borderBottom: '1px solid var(--color-border)',
    }}>
      <Skeleton style={{ height: 10, width: 140 }} />
      <Skeleton style={{ height: 10, width: 100 }} />
      <Skeleton style={{ height: 10, width: 80 }} />
      <Skeleton style={{ height: 10, width: 60 }} />
      <Skeleton style={{ height: 10, width: 30, marginLeft: 'auto' }} />
    </div>
    {/* Row skeletons */}
    {Array.from({ length: rows }).map((_, i) => (
      <div key={i} style={{
        display: 'flex', gap: 16, alignItems: 'center',
        padding: '14px 20px',
        borderBottom: i < rows - 1 ? '1px solid var(--color-border)' : 'none',
        opacity: 1 - (i * 0.1),
      }}>
        <Skeleton style={{ height: 14, width: 160 + Math.random() * 40 }} />
        <Skeleton style={{ height: 14, width: 100 + Math.random() * 30 }} />
        <Skeleton style={{ height: 14, width: 70 }} />
        <Skeleton style={{ height: 22, width: 56, borderRadius: 11 }} />
        <Skeleton style={{ height: 14, width: 24, marginLeft: 'auto' }} />
      </div>
    ))}
  </div>
);

export const CardSkeleton: React.FC = () => (
  <div className="card" style={{ padding: '20px 24px', display: 'flex', flexDirection: 'column', gap: 12 }}>
    <Skeleton style={{ height: 10, width: 80 }} />
    <Skeleton style={{ height: 28, width: 60 }} />
    <Skeleton style={{ height: 10, width: 100 }} />
  </div>
);

export const FormSkeleton: React.FC = () => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
    <div className="card" style={{ padding: 20 }}>
      <Skeleton style={{ height: 12, width: 120, marginBottom: 16 }} />
      <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
        <div>
          <Skeleton style={{ height: 10, width: 60, marginBottom: 8 }} />
          <Skeleton style={{ height: 32, width: '100%' }} />
        </div>
        <div>
          <Skeleton style={{ height: 10, width: 80, marginBottom: 8 }} />
          <Skeleton style={{ height: 32, width: '100%' }} />
        </div>
      </div>
    </div>
  </div>
);
