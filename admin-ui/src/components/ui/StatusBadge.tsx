import React from 'react';

const STATUS_MAP: Record<string, { label: string; bg: string; color: string; dot: string }> = {
  active:   { label: 'Active',   bg: 'rgba(34,197,94,0.1)',   color: '#22c55e', dot: '#22c55e' },
  draft:    { label: 'Draft',    bg: 'var(--color-bg-subtle)', color: 'var(--color-text-secondary)', dot: 'var(--color-text-tertiary)' },
  inactive: { label: 'Inactive', bg: 'rgba(245,158,11,0.1)',  color: '#f59e0b', dot: '#f59e0b' },
  archived: { label: 'Archived', bg: 'rgba(239,68,68,0.1)',   color: '#ef4444', dot: '#ef4444' },
};

interface Props { status: string; showDot?: boolean; }

export const StatusBadge: React.FC<Props> = ({ status, showDot = true }) => {
  const cfg = STATUS_MAP[status] ?? STATUS_MAP.draft;
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 5,
      padding: '2px 8px', borderRadius: 4,
      background: cfg.bg, color: cfg.color,
      fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.04em',
    }}>
      {showDot && (
        <span style={{
          width: 5, height: 5, borderRadius: '50%', flexShrink: 0,
          background: cfg.dot,
          boxShadow: status === 'active' ? `0 0 4px ${cfg.dot}` : 'none',
        }} />
      )}
      {cfg.label}
    </span>
  );
};
