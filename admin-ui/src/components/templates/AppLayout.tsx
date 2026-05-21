import React, { useState, useEffect } from 'react';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import {
  LayoutDashboard, Plug, Zap, History, Settings,
  ChevronLeft, Menu, X, Bell, Search,
} from 'lucide-react';

const NAV = [
  { key: '/dashboard', icon: LayoutDashboard, label: 'Dashboard' },
  { key: '/integrations', icon: Plug, label: 'Integrations' },
  { key: '/executions', icon: Zap, label: 'Executions' },
  { key: '/audit', icon: History, label: 'Audit Logs' },
  { key: '/settings', icon: Settings, label: 'Settings' },
];

export const AppLayout: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const navigate = useNavigate();
  const { pathname } = useLocation();

  useEffect(() => {
    const check = () => setIsMobile(window.innerWidth < 768);
    check();
    window.addEventListener('resize', check);
    return () => window.removeEventListener('resize', check);
  }, []);

  // Close mobile sidebar on navigate
  useEffect(() => { setMobileOpen(false); }, [pathname]);

  const activeKey = NAV.find(n => pathname.startsWith(n.key))?.key ?? '/dashboard';
  const activeLabel = NAV.find(n => n.key === activeKey)?.label ?? '';
  const sidebarW = isMobile ? 260 : collapsed ? 60 : 240;

  const SidebarContent = () => (
    <>
      {/* Logo */}
      <div style={{
        height: 56, display: 'flex', alignItems: 'center',
        padding: '0 20px', gap: 12, flexShrink: 0,
        borderBottom: '1px solid var(--color-border)',
      }}>
        <div style={{
          width: 28, height: 28, borderRadius: 8, flexShrink: 0,
          background: 'linear-gradient(135deg, #6366f1 0%, #8b5cf6 50%, #a78bfa 100%)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontSize: 11, fontWeight: 800, color: '#fff', letterSpacing: '-0.02em',
          boxShadow: '0 2px 8px rgba(99,102,241,0.3)',
        }}>IP</div>
        {(!collapsed || isMobile) && (
          <span style={{ fontSize: 14, fontWeight: 700, color: 'var(--color-text-primary)', letterSpacing: '-0.02em' }}>
            IntegrationHub
          </span>
        )}
      </div>

      {/* Nav */}
      <nav style={{ flex: 1, padding: '12px 8px', overflowY: 'auto' }}>
        {NAV.map(({ key, icon: Icon, label }) => {
          const active = activeKey === key;
          return (
            <button
              key={key}
              onClick={() => navigate(key)}
              style={{
                width: '100%', display: 'flex', alignItems: 'center',
                gap: 12, padding: collapsed && !isMobile ? '10px 14px' : '10px 14px',
                justifyContent: collapsed && !isMobile ? 'center' : 'flex-start',
                borderRadius: 8, cursor: 'pointer', border: 'none', marginBottom: 2,
                background: active ? 'var(--color-brand-subtle)' : 'transparent',
                color: active ? 'var(--color-brand-hover)' : 'var(--color-text-secondary)',
                fontSize: 13, fontWeight: active ? 600 : 400,
                transition: 'all 120ms ease', fontFamily: 'var(--font-sans)',
                position: 'relative',
              }}
              onMouseEnter={e => { if (!active) { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-primary)'; } }}
              onMouseLeave={e => { if (!active) { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-secondary)'; } }}
            >
              {active && (
                <div style={{
                  position: 'absolute', left: 0, top: '50%', transform: 'translateY(-50%)',
                  width: 3, height: 20, borderRadius: 2,
                  background: 'var(--color-brand)',
                }} />
              )}
              <Icon size={16} style={{ flexShrink: 0 }} />
              {(!collapsed || isMobile) && <span>{label}</span>}
            </button>
          );
        })}
      </nav>

      {/* Collapse (desktop only) */}
      {!isMobile && (
        <div style={{ padding: 8, borderTop: '1px solid var(--color-border)', flexShrink: 0 }}>
          <button
            onClick={() => setCollapsed(!collapsed)}
            style={{
              width: '100%', display: 'flex', alignItems: 'center',
              justifyContent: 'center', gap: 8, padding: 10, borderRadius: 8,
              border: 'none', background: 'transparent', color: 'var(--color-text-tertiary)',
              fontSize: 12, cursor: 'pointer', fontFamily: 'var(--font-sans)',
              transition: 'all 120ms ease',
            }}
            onMouseEnter={e => { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-secondary)'; }}
            onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-tertiary)'; }}
          >
            <ChevronLeft size={14} style={{ transform: collapsed ? 'rotate(180deg)' : 'none', transition: 'transform 200ms ease' }} />
            {!collapsed && <span>Collapse</span>}
          </button>
        </div>
      )}
    </>
  );

  return (
    <div className="noise" style={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      {/* Mobile overlay */}
      {isMobile && mobileOpen && (
        <div className="sidebar-overlay" onClick={() => setMobileOpen(false)} />
      )}

      {/* Sidebar */}
      <aside style={{
        width: sidebarW, flexShrink: 0,
        background: 'var(--color-bg-elevated)',
        borderRight: '1px solid var(--color-border)',
        display: 'flex', flexDirection: 'column',
        transition: 'all 250ms cubic-bezier(0.4, 0, 0.2, 1)',
        ...(isMobile ? {
          position: 'fixed', top: 0, left: 0, bottom: 0, zIndex: 50,
          transform: mobileOpen ? 'translateX(0)' : 'translateX(-100%)',
        } : {}),
      }}>
        <SidebarContent />
      </aside>

      {/* Main */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, overflow: 'hidden' }}>
        {/* Header */}
        <header className="glass" style={{
          height: 56, display: 'flex', alignItems: 'center',
          justifyContent: 'space-between', padding: '0 24px',
          borderBottom: '1px solid var(--color-border)', flexShrink: 0,
          position: 'sticky', top: 0, zIndex: 30,
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            {isMobile && (
              <button onClick={() => setMobileOpen(true)} style={{
                width: 34, height: 34, borderRadius: 8, border: 'none',
                background: 'var(--color-bg-subtle)', color: 'var(--color-text-secondary)',
                cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
              }}>
                <Menu size={16} />
              </button>
            )}
            <h1 style={{ fontSize: 16, fontWeight: 600, margin: 0, letterSpacing: '-0.01em' }}>
              {activeLabel}
            </h1>
          </div>
          <div style={{ display: 'flex', gap: 6 }}>
            <HeaderBtn><Search size={15} /></HeaderBtn>
            <HeaderBtn><Bell size={15} /></HeaderBtn>
            <div style={{
              width: 30, height: 30, borderRadius: '50%',
              background: 'linear-gradient(135deg, var(--color-brand), #8b5cf6)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: 11, fontWeight: 700, color: '#fff', cursor: 'pointer',
            }}>A</div>
          </div>
        </header>

        {/* Content */}
        <main className="main-content" style={{ flex: 1, overflowY: 'auto', padding: 28 }}>
          <Outlet />
        </main>
      </div>
    </div>
  );
};

const HeaderBtn: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <button style={{
    width: 34, height: 34, borderRadius: 8, border: 'none',
    background: 'transparent', color: 'var(--color-text-secondary)',
    cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
    transition: 'all 120ms ease',
  }}
    onMouseEnter={e => { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-primary)'; }}
    onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-secondary)'; }}
  >
    {children}
  </button>
);
