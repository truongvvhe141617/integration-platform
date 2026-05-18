import React from 'react';

export const Skeleton: React.FC<{ style?: React.CSSProperties }> = ({ style }) => (
  <div style={{
    background: 'var(--color-bg-subtle)',
    borderRadius: 4, animation: 'pulse 1.5s ease-in-out infinite',
    ...style,
  }} />
);

export const TableSkeleton: React.FC = () => (
  <div style={{ padding: 16, display: 'flex', flexDirection: 'column', gap: 12 }}>
    {Array.from({ length: 6 }).map((_, i) => (
      <div key={i} style={{ display: 'flex', gap: 16, alignItems: 'center' }}>
        <Skeleton style={{ height: 14, width: 180 }} />
        <Skeleton style={{ height: 14, width: 120 }} />
        <Skeleton style={{ height: 14, width: 80 }} />
        <Skeleton style={{ height: 20, width: 60, borderRadius: 10 }} />
        <Skeleton style={{ height: 14, width: 30, marginLeft: 'auto' }} />
      </div>
    ))}
  </div>
);
