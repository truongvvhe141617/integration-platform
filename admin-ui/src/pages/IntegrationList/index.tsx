import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Plus, Search, MoreHorizontal, Edit, Trash2,
  Play, Copy, Eye, Power, Plug, Zap, Globe, Activity,
  CheckCircle, Clock,
} from 'lucide-react';
import { useIntegrations, useDeleteIntegration, useChangeStatus } from '../../hooks/useIntegrations';
import { IntegrationConfig } from '../../api/integrations';
import { StatusBadge } from '../../components/ui/StatusBadge';
import { MetricCard } from '../../components/ui/MetricCard';
import { EmptyState } from '../../components/ui/EmptyState';
import { TableSkeleton } from '../../components/ui/Skeleton';

// ── Connector badge ──
const CONNECTOR_COLORS: Record<string, { bg: string; color: string; border: string }> = {
  'generic-http': { bg: 'rgba(99,102,241,0.1)', color: '#818cf8', border: 'rgba(99,102,241,0.2)' },
  'bank-e':       { bg: 'rgba(245,158,11,0.1)', color: '#f59e0b', border: 'rgba(245,158,11,0.2)' },
  'custom':       { bg: 'rgba(34,197,94,0.1)',  color: '#22c55e', border: 'rgba(34,197,94,0.2)' },
};
const defaultConnector = { bg: 'var(--color-bg-subtle)', color: 'var(--color-text-secondary)', border: 'var(--color-border)' };

// ── Action dropdown ──
const ActionMenu: React.FC<{
  item: IntegrationConfig;
  onView: () => void; onEdit: () => void; onTest: () => void;
  onToggle: () => void; onDelete: () => void;
}> = ({ item, onView, onEdit, onTest, onToggle, onDelete }) => {
  const [open, setOpen] = useState(false);

  return (
    <div style={{ position: 'relative' }}>
      <button
        onClick={e => { e.stopPropagation(); setOpen(o => !o); }}
        style={{
          width: 28, height: 28, borderRadius: 4, border: 'none',
          background: 'transparent', color: 'var(--color-text-tertiary)',
          cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
          transition: 'all 100ms ease',
        }}
        onMouseEnter={e => { e.currentTarget.style.background = 'var(--color-bg-hover)'; e.currentTarget.style.color = 'var(--color-text-primary)'; }}
        onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = 'var(--color-text-tertiary)'; }}
      >
        <MoreHorizontal size={15} />
      </button>

      {open && (
        <>
          <div style={{ position: 'fixed', inset: 0, zIndex: 40 }} onClick={() => setOpen(false)} />
          <div style={{
            position: 'absolute', right: 0, top: 32, zIndex: 50,
            width: 168, background: 'var(--color-bg-overlay)',
            border: '1px solid var(--color-border)', borderRadius: 8,
            boxShadow: '0 8px 24px rgba(0,0,0,0.6)', padding: '4px 0',
            animation: 'var(--animate-fade-in)',
          }}>
            {[
              { icon: Eye, label: 'View', action: onView, danger: false },
              { icon: Edit, label: 'Edit', action: onEdit, danger: false },
              { icon: Play, label: 'Test', action: onTest, danger: false },
              { icon: Copy, label: 'Duplicate', action: () => {}, danger: false },
            ].map(({ icon: Icon, label, action }) => (
              <MenuItem key={label} icon={<Icon size={13} />} label={label}
                onClick={() => { action(); setOpen(false); }} />
            ))}
            <div style={{ margin: '4px 0', borderTop: '1px solid var(--color-border)' }} />
            <MenuItem
              icon={<Power size={13} />}
              label={item.status === 'active' ? 'Deactivate' : 'Activate'}
              onClick={() => { onToggle(); setOpen(false); }}
            />
            <div style={{ margin: '4px 0', borderTop: '1px solid var(--color-border)' }} />
            <MenuItem icon={<Trash2 size={13} />} label="Delete" danger
              onClick={() => { onDelete(); setOpen(false); }} />
          </div>
        </>
      )}
    </div>
  );
};

const MenuItem: React.FC<{ icon: React.ReactNode; label: string; onClick: () => void; danger?: boolean }> = ({ icon, label, onClick, danger }) => (
  <button
    onClick={onClick}
    style={{
      width: '100%', display: 'flex', alignItems: 'center', gap: 8,
      padding: '7px 12px', border: 'none', background: 'transparent',
      color: danger ? 'var(--color-danger)' : 'var(--color-text-secondary)',
      fontSize: 13, cursor: 'pointer', fontFamily: 'var(--font-sans)',
      transition: 'all 100ms ease', textAlign: 'left',
    }}
    onMouseEnter={e => { e.currentTarget.style.background = danger ? 'rgba(239,68,68,0.1)' : 'var(--color-bg-hover)'; e.currentTarget.style.color = danger ? 'var(--color-danger)' : 'var(--color-text-primary)'; }}
    onMouseLeave={e => { e.currentTarget.style.background = 'transparent'; e.currentTarget.style.color = danger ? 'var(--color-danger)' : 'var(--color-text-secondary)'; }}
  >
    {icon}{label}
  </button>
);

// ── Main page ──
export const IntegrationListPage: React.FC = () => {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  const { data, isLoading } = useIntegrations({ search, status: statusFilter || undefined });
  const deleteMutation = useDeleteIntegration();
  const statusMutation = useChangeStatus();

  const items = data?.items ?? [];
  const total = data?.total ?? 0;

  const metrics = useMemo(() => ({
    total,
    active: items.filter(i => i.status === 'active').length,
    draft: items.filter(i => i.status === 'draft').length,
    connectors: new Set(items.map(i => i.connectorType)).size,
  }), [items, total]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <div>
          <h1 style={{ fontSize: 22, fontWeight: 700, color: 'var(--color-text-primary)', margin: 0 }}>
            Integrations
          </h1>
          <p style={{ fontSize: 13, color: 'var(--color-text-secondary)', margin: '4px 0 0' }}>
            Manage connector configurations and API integrations
          </p>
        </div>
        <button onClick={() => navigate('/integrations/new')} className="btn btn-md btn-primary">
          <Plus size={14} /> New Integration
        </button>
      </div>

      {/* Metrics */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12 }}>
        <MetricCard label="Total" value={metrics.total} sub="All integrations" icon={<Plug size={14} />} />
        <MetricCard label="Active" value={metrics.active} sub="Running" icon={<CheckCircle size={14} style={{ color: '#22c55e' }} />} />
        <MetricCard label="Draft" value={metrics.draft} sub="Pending review" icon={<Clock size={14} style={{ color: '#f59e0b' }} />} />
        <MetricCard label="Connectors" value={metrics.connectors} sub="Unique types" icon={<Zap size={14} style={{ color: '#6366f1' }} />} />
      </div>

      {/* Table */}
      <div className="card" style={{ overflow: 'hidden' }}>
        {/* Toolbar */}
        <div style={{
          display: 'flex', alignItems: 'center', gap: 10,
          padding: '12px 16px', borderBottom: '1px solid var(--color-border)',
        }}>
          <div style={{ position: 'relative', flex: 1, maxWidth: 280 }}>
            <Search size={13} style={{
              position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)',
              color: 'var(--color-text-tertiary)', pointerEvents: 'none',
            }} />
            <input
              className="input"
              style={{ paddingLeft: 32 }}
              placeholder="Search integrations..."
              value={search}
              onChange={e => setSearch(e.target.value)}
            />
          </div>

          <select
            className="input"
            style={{ width: 130, cursor: 'pointer' }}
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value)}
          >
            <option value="">All status</option>
            <option value="active">Active</option>
            <option value="draft">Draft</option>
            <option value="inactive">Inactive</option>
            <option value="archived">Archived</option>
          </select>

          <span style={{ marginLeft: 'auto', fontSize: 12, color: 'var(--color-text-tertiary)' }}>
            {total} integration{total !== 1 ? 's' : ''}
          </span>
        </div>

        {/* Content */}
        {isLoading ? (
          <TableSkeleton />
        ) : items.length === 0 ? (
          <EmptyState
            icon={<Plug size={22} />}
            title="No integrations yet"
            description="Create your first integration to connect with external APIs and services"
            action={{ label: 'New Integration', onClick: () => navigate('/integrations/new') }}
          />
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--color-border)', background: 'var(--color-bg-subtle)' }}>
                  {['Integration', 'Endpoint', 'Type', 'Status', 'Ops', 'Version', ''].map(h => (
                    <th key={h} style={{
                      padding: '10px 16px', textAlign: 'left',
                      fontSize: 10, fontWeight: 700, color: 'var(--color-text-secondary)',
                      textTransform: 'uppercase', letterSpacing: '0.08em', whiteSpace: 'nowrap',
                    }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {items.map((item, idx) => {
                  const cs = CONNECTOR_COLORS[item.connectorType] ?? defaultConnector;
                  return (
                    <tr
                      key={item.id}
                      style={{ borderBottom: idx < items.length - 1 ? '1px solid rgba(42,42,42,0.6)' : 'none' }}
                      onMouseEnter={e => (e.currentTarget.style.background = 'var(--color-bg-hover)')}
                      onMouseLeave={e => (e.currentTarget.style.background = 'transparent')}
                    >
                      {/* Name */}
                      <td style={{ padding: '12px 16px' }}>
                        <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                          <button
                            onClick={() => navigate(`/integrations/${item.id}`)}
                            style={{
                              background: 'none', border: 'none', padding: 0, cursor: 'pointer',
                              fontSize: 13, fontWeight: 500, color: 'var(--color-text-primary)',
                              textAlign: 'left', fontFamily: 'var(--font-sans)',
                              transition: 'color 100ms ease',
                            }}
                            onMouseEnter={e => (e.currentTarget.style.color = '#818cf8')}
                            onMouseLeave={e => (e.currentTarget.style.color = 'var(--color-text-primary)')}
                          >
                            {item.name}
                          </button>
                          <span style={{ fontSize: 11, color: 'var(--color-text-tertiary)', fontFamily: 'var(--font-mono)' }}>
                            {item.configKey}
                          </span>
                        </div>
                      </td>

                      {/* Endpoint */}
                      <td style={{ padding: '12px 16px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                          <Globe size={12} style={{ color: 'var(--color-text-tertiary)', flexShrink: 0 }} />
                          <span style={{ fontSize: 12, color: 'var(--color-text-secondary)', fontFamily: 'var(--font-mono)', maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                            {item.baseUrl.replace(/^https?:\/\//, '')}
                          </span>
                        </div>
                      </td>

                      {/* Type */}
                      <td style={{ padding: '12px 16px' }}>
                        <span style={{
                          display: 'inline-flex', alignItems: 'center',
                          padding: '2px 8px', borderRadius: 4,
                          background: cs.bg, color: cs.color,
                          border: `1px solid ${cs.border}`,
                          fontSize: 11, fontWeight: 500,
                        }}>
                          {item.connectorType}
                        </span>
                      </td>

                      {/* Status */}
                      <td style={{ padding: '12px 16px' }}>
                        <StatusBadge status={item.status} />
                      </td>

                      {/* Ops */}
                      <td style={{ padding: '12px 16px' }}>
                        <span style={{
                          display: 'inline-flex', alignItems: 'center', gap: 4,
                          fontSize: 11, fontWeight: 600, color: 'var(--color-text-secondary)',
                          background: 'var(--color-bg-subtle)', padding: '2px 8px', borderRadius: 4,
                        }}>
                          <Activity size={11} />
                          {item.operations?.length ?? 0}
                        </span>
                      </td>

                      {/* Version */}
                      <td style={{ padding: '12px 16px' }}>
                        <span style={{ fontSize: 11, color: 'var(--color-text-tertiary)', fontFamily: 'var(--font-mono)' }}>
                          v{item.version}
                        </span>
                      </td>

                      {/* Actions */}
                      <td style={{ padding: '12px 16px' }}>
                        <ActionMenu
                          item={item}
                          onView={() => navigate(`/integrations/${item.id}`)}
                          onEdit={() => navigate(`/integrations/${item.id}/edit`)}
                          onTest={() => navigate(`/integrations/${item.id}/test`)}
                          onToggle={() => statusMutation.mutate({ id: item.id, status: item.status === 'active' ? 'inactive' : 'active' })}
                          onDelete={() => deleteMutation.mutate(item.id)}
                        />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
