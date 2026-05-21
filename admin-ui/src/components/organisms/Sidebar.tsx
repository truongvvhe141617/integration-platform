import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  LayoutDashboard, Plug, Zap, History, Settings,
  ChevronLeft, ChevronRight,
} from 'lucide-react';

const NAV_ITEMS = [
  { key: '/dashboard',    icon: LayoutDashboard, label: 'Dashboard' },
  { key: '/integrations', icon: Plug,            label: 'Integrations' },
  { key: '/executions',   icon: Zap,             label: 'Executions' },
  { key: '/audit',        icon: History,         label: 'Audit Logs' },
  { key: '/settings',     icon: Settings,        label: 'Settings' },
];

interface SidebarProps {
  collapsed: boolean;
  onToggle: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ collapsed, onToggle }) => {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const activeKey = NAV_ITEMS.find((n) => pathname.startsWith(n.key))?.key ?? '/integrations';

  return (
    <aside style={{
      width: collapsed ? 56 : 220, flexShrink: 0,
      background: 'var(--color-bg-elevated)',
      borderRight: '1px solid var(--color-border)',
      display: 'flex', flexDirection: 'column',
      transition: 'width 200ms cubic-bezier(0.4, 0, 0.2, 1)',
      overflow: 'hidden',
    }}>
      {/* Logo */}
      <div style={{
        height: 52, display: 'flex', alignItems: 'center',
        padding: collapsed ? '0 16px' : '0 20px',
        justifyContent: collapsed ? 'center' : 'flex-start',
        borderBottom: '1px solid var(--color-border)',
        gap: 10, flexShrink: 0,
      }}>
        <div style={{
          width: 26, height: 26, borderRadius: 7, flexShrink: 0,
          background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontSize: 10, fontWeight: 700, color: '#fff',
        }}>IP</div>
        {!collapsed && (
          <span style={{
            fontSize: 13, fontWeight: 600, color: 'var(--color-text-primary)',
            whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
          }}>
            Integration Platform
          </span>
        )}
      </div>

      {/* Navigation */}
      <nav style={{ flex: 1, padding: '8px 0', overflowY: 'auto', overflowX: 'hidden' }}>
        {NAV_ITEMS.map(({ key, icon: Icon, label }) => {
          const isActive = activeKey === key;
          return (
            <button
              key={key}
              onClick={() => navigate(key)}
              title={collapsed ? label : undefined}
              style={{
                width: 'calc(100% - 16px)', margin: '1px 8px',
                display: 'flex', alignItems: 'center',
                gap: 10, padding: '9px 12px',
                justifyContent: collapsed ? 'center' : 'flex-start',
                borderRadius: 6, cursor: 'pointer', border: 'none',
                background: isActive ? 'rgba(99,102,241,0.12)' : 'transparent',
                color: isActive ? '#818cf8' : 'var(--color-text-secondary)',
                fontSize: 13, fontWeight: isActive ? 500 : 400,
                transition: 'all 100ms ease', fontFamily: 'var(--font-sans)',
              }}
              onMouseEnter={(e) => { if (!isActive) { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-primary)'; } }}
              onMouseLeave={(e) => { if (!isActive) { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-secondary)'; } }}
            >
              <Icon size={15} style={{ flexShrink: 0 }} />
              {!collapsed && <span style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{label}</span>}
            </button>
          );
        })}
      </nav>

      {/* Collapse toggle */}
      <div style={{ padding: 8, borderTop: '1px solid var(--color-border)', flexShrink: 0 }}>
        <button
          onClick={onToggle}
          style={{
            width: '100%', display: 'flex', alignItems: 'center',
            justifyContent: collapsed ? 'center' : 'flex-start',
            gap: 8, padding: 8, borderRadius: 6, border: 'none',
            background: 'transparent', color: 'var(--color-text-tertiary)',
            fontSize: 12, cursor: 'pointer', fontFamily: 'var(--font-sans)',
            transition: 'all 100ms ease',
          }}
          onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-secondary)'; }}
          onMouseLeave={(e) => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-tertiary)'; }}
        >
          {collapsed ? <ChevronRight size={14} /> : <><ChevronLeft size={14} /><span>Collapse</span></>}
        </button>
      </div>
    </aside>
  );
};
