import React from 'react';

interface Props {
  label: string;
  value: string | number;
  sub?: string;
  icon?: React.ReactNode;
}

export const MetricCard: React.FC<Props> = ({ label, value, sub, icon }) => (
  <div
    className="card-hover"
    style={{ padding: '20px 24px', display: 'flex', flexDirection: 'column', gap: 8 }}
  >
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
      <span style={{ fontSize: 11, color: 'var(--color-text-secondary)', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.06em' }}>
        {label}
      </span>
      {icon && <span style={{ color: 'var(--color-text-tertiary)', opacity: 0.8 }}>{icon}</span>}
    </div>
    <span style={{ fontSize: 28, fontWeight: 700, color: 'var(--color-text-primary)', lineHeight: 1, fontVariantNumeric: 'tabular-nums' }}>
      {value}
    </span>
    {sub && <span style={{ fontSize: 12, color: 'var(--color-text-tertiary)' }}>{sub}</span>}
  </div>
);
