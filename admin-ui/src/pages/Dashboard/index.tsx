import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Plug, Zap, CheckCircle, AlertTriangle, Clock, ArrowRight } from 'lucide-react';
import { useIntegrations } from '../../hooks/useIntegrations';

export const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const { data } = useIntegrations();
  const items = data?.items ?? [];

  const total = items.length;
  const active = items.filter(i => i.status === 'active').length;
  const draft = items.filter(i => i.status === 'draft').length;
  const inactive = items.filter(i => i.status === 'inactive').length;

  return (
    <div className="page-container">
      <div className="page-header">
        <h1 className="page-title">Dashboard</h1>
        <p className="page-subtitle">Tổng quan hệ thống Integration Platform</p>
      </div>

      {/* Metrics */}
      <div className="grid-metrics" style={{ marginBottom: 28 }}>
        <Metric icon={<Plug size={18} />} label="Tổng Integration" value={total} color="var(--color-brand)" />
        <Metric icon={<CheckCircle size={18} />} label="Đang hoạt động" value={active} color="var(--color-success)" />
        <Metric icon={<Clock size={18} />} label="Bản nháp" value={draft} color="var(--color-text-secondary)" />
        <Metric icon={<AlertTriangle size={18} />} label="Tạm dừng" value={inactive} color="var(--color-warning)" />
      </div>

      {/* Quick actions */}
      <div className="card" style={{ padding: 24, marginBottom: 24 }}>
        <h3 style={{ fontSize: 14, fontWeight: 600, margin: '0 0 16px 0' }}>Thao tác nhanh</h3>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12 }}>
          <QuickAction
            icon={<Plug size={16} />}
            title="Tạo Integration"
            desc="Kết nối API đối tác mới"
            onClick={() => navigate('/integrations/new')}
          />
          <QuickAction
            icon={<Zap size={16} />}
            title="Xem Executions"
            desc="Lịch sử gọi API"
            onClick={() => navigate('/executions')}
          />
          <QuickAction
            icon={<CheckCircle size={16} />}
            title="Quản lý Integrations"
            desc="Danh sách cấu hình"
            onClick={() => navigate('/integrations')}
          />
        </div>
      </div>

      {/* Recent integrations */}
      {items.length > 0 && (
        <div className="card" style={{ padding: 24 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
            <h3 style={{ fontSize: 14, fontWeight: 600, margin: 0 }}>Integrations gần đây</h3>
            <button className="btn btn-ghost btn-sm" onClick={() => navigate('/integrations')}>
              Xem tất cả <ArrowRight size={12} />
            </button>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 0 }}>
            {items.slice(0, 5).map(item => (
              <div key={item.id} style={{
                display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                padding: '10px 0', borderBottom: '1px solid var(--color-border)',
              }}>
                <div>
                  <div style={{ fontSize: 13, fontWeight: 500 }}>{item.name}</div>
                  <div style={{ fontSize: 11, color: 'var(--color-text-tertiary)', fontFamily: 'var(--font-mono)' }}>{item.configKey}</div>
                </div>
                <span style={{
                  fontSize: 10, fontWeight: 600, textTransform: 'uppercase',
                  padding: '2px 6px', borderRadius: 3,
                  background: item.status === 'active' ? 'rgba(34,197,94,0.1)' : 'var(--color-bg-subtle)',
                  color: item.status === 'active' ? 'var(--color-success)' : 'var(--color-text-secondary)',
                }}>
                  {item.status}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

function Metric({ icon, label, value, color }: { icon: React.ReactNode; label: string; value: number; color: string }) {
  return (
    <div className="card" style={{ padding: '16px 20px' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
        <span style={{ color }}>{icon}</span>
        <span style={{ fontSize: 11, color: 'var(--color-text-secondary)', fontWeight: 500 }}>{label}</span>
      </div>
      <div style={{ fontSize: 28, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>{value}</div>
    </div>
  );
}

function QuickAction({ icon, title, desc, onClick }: { icon: React.ReactNode; title: string; desc: string; onClick: () => void }) {
  return (
    <button onClick={onClick} style={{
      display: 'flex', alignItems: 'flex-start', gap: 12, padding: 14,
      background: 'var(--color-bg-subtle)', border: '1px solid var(--color-border)',
      borderRadius: 8, cursor: 'pointer', textAlign: 'left',
      transition: 'all 100ms ease', fontFamily: 'var(--font-sans)',
    }}
      onMouseEnter={e => { e.currentTarget.style.borderColor = 'var(--color-border-strong)'; e.currentTarget.style.background = 'var(--color-bg-hover)'; }}
      onMouseLeave={e => { e.currentTarget.style.borderColor = 'var(--color-border)'; e.currentTarget.style.background = 'var(--color-bg-subtle)'; }}
    >
      <span style={{ color: 'var(--color-brand)', marginTop: 2 }}>{icon}</span>
      <div>
        <div style={{ fontSize: 13, fontWeight: 500, color: 'var(--color-text-primary)' }}>{title}</div>
        <div style={{ fontSize: 11, color: 'var(--color-text-tertiary)', marginTop: 2 }}>{desc}</div>
      </div>
    </button>
  );
}
