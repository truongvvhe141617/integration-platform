import React from 'react';

type BadgeVariant = 'active' | 'draft' | 'inactive' | 'archived' | 'info' | 'warning';

const VARIANTS: Record<BadgeVariant, { bg: string; color: string; dot: string }> = {
  active:   { bg: 'rgba(34,197,94,0.1)',   color: '#22c55e', dot: '#22c55e' },
  draft:    { bg: 'var(--color-bg-subtle)', color: 'var(--color-text-secondary)', dot: 'var(--color-text-tertiary)' },
  inactive: { bg: 'rgba(245,158,11,0.1)',  color: '#f59e0b', dot: '#f59e0b' },
  archived: { bg: 'rgba(239,68,68,0.1)',   color: '#ef4444', dot: '#ef4444' },
  info:     { bg: 'rgba(59,130,246,0.1)',  color: '#3b82f6', dot: '#3b82f6' },
  warning:  { bg: 'rgba(245,158,11,0.1)',  color: '#f59e0b', dot: '#f59e0b' },
};

interface BadgeProps {
  variant: BadgeVariant;
  children: React.ReactNode;
  dot?: boolean;
}

export const Badge: React.FC<BadgeProps> = ({ variant, children, dot = true }) => {
  const cfg = VARIANTS[variant] ?? VARIANTS.draft;
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 5,
      padding: '2px 8px', borderRadius: 4,
      background: cfg.bg, color: cfg.color,
      fontSize: 11, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.04em',
    }}>
      {dot && (
        <span style={{
          width: 5, height: 5, borderRadius: '50%', flexShrink: 0,
          background: cfg.dot,
          boxShadow: variant === 'active' ? `0 0 4px ${cfg.dot}` : 'none',
        }} />
      )}
      {children}
    </span>
  );
};
