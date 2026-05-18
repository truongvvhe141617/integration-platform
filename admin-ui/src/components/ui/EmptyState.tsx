import React from 'react';

interface Props {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: { label: string; onClick: () => void };
}

export const EmptyState: React.FC<Props> = ({ icon, title, description, action }) => (
  <div style={{
    display: 'flex', flexDirection: 'column', alignItems: 'center',
    justifyContent: 'center', padding: '64px 24px', gap: 12,
  }}>
    {icon && (
      <div style={{
        width: 48, height: 48, borderRadius: 12,
        background: 'var(--color-bg-subtle)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        color: 'var(--color-text-tertiary)', marginBottom: 4,
      }}>
        {icon}
      </div>
    )}
    <p style={{ fontSize: 14, fontWeight: 600, color: 'var(--color-text-primary)', margin: 0 }}>{title}</p>
    {description && (
      <p style={{ fontSize: 13, color: 'var(--color-text-secondary)', margin: 0, textAlign: 'center', maxWidth: 360 }}>
        {description}
      </p>
    )}
    {action && (
      <button onClick={action.onClick} className="btn btn-md btn-primary" style={{ marginTop: 8 }}>
        {action.label}
      </button>
    )}
  </div>
);
