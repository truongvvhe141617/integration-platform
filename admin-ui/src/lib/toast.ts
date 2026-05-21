/**
 * Simple toast notification system.
 * Không phụ thuộc thư viện ngoài.
 */

type ToastType = 'success' | 'error' | 'warning' | 'info';

interface ToastOptions {
  duration?: number;
}

const COLORS: Record<ToastType, { bg: string; border: string; color: string; icon: string }> = {
  success: { bg: '#0a2e1a', border: '#22c55e40', color: '#22c55e', icon: '✓' },
  error:   { bg: '#2e0a0a', border: '#ef444440', color: '#ef4444', icon: '✗' },
  warning: { bg: '#2e1f0a', border: '#f59e0b40', color: '#f59e0b', icon: '⚠' },
  info:    { bg: '#0a1a2e', border: '#3b82f640', color: '#3b82f6', icon: 'ℹ' },
};

let container: HTMLDivElement | null = null;

function getContainer() {
  if (container) return container;
  container = document.createElement('div');
  container.id = 'toast-container';
  Object.assign(container.style, {
    position: 'fixed', top: '16px', right: '16px', zIndex: '9999',
    display: 'flex', flexDirection: 'column', gap: '8px',
    pointerEvents: 'none',
  });
  document.body.appendChild(container);
  return container;
}

function show(type: ToastType, message: string, options?: ToastOptions) {
  const { duration = 4000 } = options ?? {};
  const cfg = COLORS[type];

  const el = document.createElement('div');
  Object.assign(el.style, {
    display: 'flex', alignItems: 'center', gap: '8px',
    padding: '10px 16px', borderRadius: '8px',
    background: cfg.bg, border: `1px solid ${cfg.border}`,
    color: cfg.color, fontSize: '13px', fontWeight: '500',
    fontFamily: "'Inter', sans-serif",
    boxShadow: '0 4px 12px rgba(0,0,0,0.4)',
    transform: 'translateX(100%)', opacity: '0',
    transition: 'all 200ms ease', pointerEvents: 'auto',
    maxWidth: '360px',
  });
  el.innerHTML = `<span style="font-size:14px">${cfg.icon}</span><span style="color:#f0f0f0">${message}</span>`;

  getContainer().appendChild(el);

  // Animate in
  requestAnimationFrame(() => {
    el.style.transform = 'translateX(0)';
    el.style.opacity = '1';
  });

  // Auto remove
  const timer = window.setTimeout(() => remove(el), duration);

  // Click to dismiss
  el.onclick = () => { clearTimeout(timer); remove(el); };
}

function remove(el: HTMLDivElement) {
  el.style.transform = 'translateX(100%)';
  el.style.opacity = '0';
  setTimeout(() => el.remove(), 200);
}

export const toast = {
  success: (msg: string, opts?: ToastOptions) => show('success', msg, opts),
  error: (msg: string, opts?: ToastOptions) => show('error', msg, opts),
  warning: (msg: string, opts?: ToastOptions) => show('warning', msg, opts),
  info: (msg: string, opts?: ToastOptions) => show('info', msg, opts),
};
