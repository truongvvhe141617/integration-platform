import React from 'react';
import { Button } from '../atoms/Button';

interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: { label: string; onClick: () => void };
}

export const EmptyState: React.FC<EmptyStateProps> = ({ icon, title, description, action }) => (
  <div style={{
    display: 'flex', flexDirection: 'column', alignItems: 'center',
    justifyContent: 'center', padding: '64px 24px', gap: 12,
  }}>
    {icon && (
      <div style={{
        width: 52, height: 52, borderRadius: 14,
        background: 'var(--color-bg-subtle)', border: '1px solid var(--color-border)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        color: 'var(--color-text-tertiary)', marginBottom: 4,
      }}>
        {icon}
      </div>
    )}
    <p style={{ fontSize: 15, fontWeight: 600, color: 'var(--color-text-primary)', margin: 0 }}>
      {title}
    </p>
    {description && (
      <p style={{ fontSize: 13, color: 'var(--color-text-secondary)', margin: 0, textAlign: 'center', maxWidth: 360, lineHeight: 1.5 }}>
        {description}
      </p>
    )}
    {action && (
      <Button variant="primary" size="md" onClick={action.onClick} style={{ marginTop: 8 }}>
        {action.label}
      </Button>
    )}
  </div>
);
