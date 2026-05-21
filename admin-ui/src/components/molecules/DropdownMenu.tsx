import React, { useState, useRef, useEffect } from 'react';
import { MoreHorizontal } from 'lucide-react';

interface MenuItem {
  icon?: React.ReactNode;
  label: string;
  onClick: () => void;
  danger?: boolean;
  divider?: boolean;
}

interface DropdownMenuProps {
  items: MenuItem[];
  trigger?: React.ReactNode;
}

export const DropdownMenu: React.FC<DropdownMenuProps> = ({ items, trigger }) => {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  return (
    <div ref={ref} style={{ position: 'relative' }}>
      <button
        onClick={(e) => { e.stopPropagation(); setOpen((o) => !o); }}
        style={{
          width: 28, height: 28, borderRadius: 'var(--radius-sm)', border: 'none',
          background: open ? 'var(--color-bg-hover)' : 'transparent',
          color: 'var(--color-text-tertiary)', cursor: 'pointer',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          transition: 'all 100ms ease',
        }}
        onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-primary)'; }}
        onMouseLeave={(e) => { if (!open) { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-tertiary)'; } }}
      >
        {trigger ?? <MoreHorizontal size={15} />}
      </button>

      {open && (
        <div style={{
          position: 'absolute', right: 0, top: 34, zIndex: 50,
          width: 176, background: 'var(--color-bg-overlay)',
          border: '1px solid var(--color-border)', borderRadius: 'var(--radius-lg)',
          boxShadow: 'var(--shadow-lg)', padding: '4px 0',
          animation: 'var(--animate-fade-in)',
        }}>
          {items.map((item, i) => (
            <React.Fragment key={i}>
              {item.divider && <div style={{ margin: '4px 0', borderTop: '1px solid var(--color-border)' }} />}
              <button
                onClick={() => { item.onClick(); setOpen(false); }}
                style={{
                  width: '100%', display: 'flex', alignItems: 'center', gap: 8,
                  padding: '7px 12px', border: 'none', background: 'transparent',
                  color: item.danger ? 'var(--color-danger)' : 'var(--color-text-secondary)',
                  fontSize: 13, cursor: 'pointer', fontFamily: 'var(--font-sans)',
                  transition: 'all 80ms ease', textAlign: 'left',
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.background = item.danger ? 'rgba(239,68,68,0.08)' : 'var(--color-bg-hover)';
                  e.currentTarget.style.color = item.danger ? 'var(--color-danger)' : 'var(--color-text-primary)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.background = 'transparent';
                  e.currentTarget.style.color = item.danger ? 'var(--color-danger)' : 'var(--color-text-secondary)';
                }}
              >
                {item.icon}
                {item.label}
              </button>
            </React.Fragment>
          ))}
        </div>
      )}
    </div>
  );
};
