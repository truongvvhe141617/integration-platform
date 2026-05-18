import React, { useState } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  LayoutDashboard, Plug, Zap, History, Settings,
  ChevronLeft, ChevronRight, Bell, User, Activity,
} from 'lucide-react';

const NAV = [
  { key: '/dashboard',    icon: LayoutDashboard, label: 'Dashboard' },
  { key: '/integrations', icon: Plug,            label: 'Integrations' },
  { key: '/executions',   icon: Zap,             label: 'Executions' },
  { key: '/audit',        icon: History,         label: 'Audit Logs' },
  { key: '/settings',     icon: Settings,        label: 'Settings' },
];

export const AppLayout: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const navigate = useNavigate();
  const { pathname } = useLocation();

  const activeKey = NAV.find(n => pathname.startsWith(n.key))?.key ?? '/integrations';
  const activeLabel = NAV.find(n => n.key === activeKey)?.label ?? '';
  const sidebarW = collapsed ? 56 : 220;

  return (
    <div style={{ display: 'flex', height: '100vh', background: 'var(--color-bg-base)', overflow: 'hidden' }}>
      {/* ── Sidebar ── */}
      <aside style={{
        width: sidebarW, flexShrink: 0,
        background: 'var(--color-bg-elevated)',
        borderRight: '1px solid var(--color-border)',
        display: 'flex', flexDirection: 'column',
        transition: 'width 200ms ease',
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
            width: 24, height: 24, borderRadius: 6, flexShrink: 0,
            background: 'linear-gradient(135deg, #6366f1, #8b5cf6)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 11, fontWeight: 700, color: '#fff',
          }}>IP</div>
          {!collapsed && (
            <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--color-text-primary)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
              Integration Platform
            </span>
          )}
        </div>

        {/* Nav */}
        <nav style={{ flex: 1, padding: '8px 0', overflowY: 'auto', overflowX: 'hidden' }}>
          {NAV.map(({ key, icon: Icon, label }) => {
            const isActive = activeKey === key;
            return (
              <button
                key={key}
                onClick={() => navigate(key)}
                title={collapsed ? label : undefined}
                style={{
                  width: 'calc(100% - 16px)', margin: '1px 8px',
                  display: 'flex', alignItems: 'center',
                  gap: 10, padding: collapsed ? '9px 12px' : '9px 12px',
                  justifyContent: collapsed ? 'center' : 'flex-start',
                  borderRadius: 6, cursor: 'pointer', border: 'none',
                  background: isActive ? 'rgba(99,102,241,0.12)' : 'transparent',
                  color: isActive ? '#818cf8' : 'var(--color-text-secondary)',
                  fontSize: 13, fontWeight: isActive ? 500 : 400,
                  transition: 'all 100ms ease', fontFamily: 'var(--font-sans)',
                }}
                onMouseEnter={e => { if (!isActive) { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-primary)'; } }}
                onMouseLeave={e => { if (!isActive) { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-secondary)'; } }}
              >
                <Icon size={15} style={{ flexShrink: 0 }} />
                {!collapsed && <span style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{label}</span>}
              </button>
            );
          })}
        </nav>

        {/* Collapse */}
        <div style={{ padding: '8px', borderTop: '1px solid var(--color-border)', flexShrink: 0 }}>
          <button
            onClick={() => setCollapsed(!collapsed)}
            style={{
              width: '100%', display: 'flex', alignItems: 'center',
              justifyContent: collapsed ? 'center' : 'flex-start',
              gap: 8, padding: '8px', borderRadius: 6, border: 'none',
              background: 'transparent', color: 'var(--color-text-tertiary)',
              fontSize: 12, cursor: 'pointer', fontFamily: 'var(--font-sans)',
              transition: 'all 100ms ease',
            }}
            onMouseEnter={e => { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-secondary)'; }}
            onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-tertiary)'; }}
          >
            {collapsed ? <ChevronRight size={14} /> : <><ChevronLeft size={14} /><span>Collapse</span></>}
          </button>
        </div>
      </aside>

      {/* ── Main ── */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, overflow: 'hidden' }}>
        {/* Header */}
        <header style={{
          height: 52, display: 'flex', alignItems: 'center',
          justifyContent: 'space-between', padding: '0 24px',
          background: 'var(--color-bg-elevated)',
          borderBottom: '1px solid var(--color-border)', flexShrink: 0,
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <Activity size={14} style={{ color: 'var(--color-success)' }} />
            <span style={{ fontSize: 14, fontWeight: 500, color: 'var(--color-text-primary)' }}>
              {activeLabel}
            </span>
          </div>
          <div style={{ display: 'flex', gap: 4 }}>
            <HeaderBtn><Bell size={15} /></HeaderBtn>
            <HeaderBtn><User size={15} /></HeaderBtn>
          </div>
        </header>

        {/* Content */}
        <main style={{ flex: 1, overflowY: 'auto', padding: 24 }}>
          <Outlet />
        </main>
      </div>
    </div>
  );
};

const HeaderBtn: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <button
    style={{
      width: 32, height: 32, borderRadius: 6, border: 'none',
      background: 'transparent', color: 'var(--color-text-secondary)',
      cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
      transition: 'all 100ms ease',
    }}
    onMouseEnter={e => { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-primary)'; }}
    onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-secondary)'; }}
  >
    {children}
  </button>
);
