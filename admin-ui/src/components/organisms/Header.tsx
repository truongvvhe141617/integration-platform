import React from 'react';
import { useLocation } from 'react-router-dom';
import { Bell, User, Activity } from 'lucide-react';

const ROUTE_LABELS: Record<string, string> = {
  '/dashboard': 'Dashboard',
  '/integrations': 'Integrations',
  '/executions': 'Executions',
  '/audit': 'Audit Logs',
  '/settings': 'Settings',
};

export const Header: React.FC = () => {
  const { pathname } = useLocation();
  const label = Object.entries(ROUTE_LABELS).find(([key]) => pathname.startsWith(key))?.[1] ?? '';

  return (
    <header style={{
      height: 52, display: 'flex', alignItems: 'center',
      justifyContent: 'space-between', padding: '0 24px',
      background: 'var(--color-bg-elevated)',
      borderBottom: '1px solid var(--color-border)', flexShrink: 0,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <Activity size={14} style={{ color: 'var(--color-success)' }} />
        <span style={{ fontSize: 14, fontWeight: 500, color: 'var(--color-text-primary)' }}>
          {label}
        </span>
      </div>
      <div style={{ display: 'flex', gap: 4 }}>
        <IconButton><Bell size={15} /></IconButton>
        <IconButton><User size={15} /></IconButton>
      </div>
    </header>
  );
};

const IconButton: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <button
    style={{
      width: 32, height: 32, borderRadius: 6, border: 'none',
      background: 'transparent', color: 'var(--color-text-secondary)',
      cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
      transition: 'all 100ms ease',
    }}
    onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-primary)'; }}
    onMouseLeave={(e) => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-secondary)'; }}
  >
    {children}
  </button>
);
