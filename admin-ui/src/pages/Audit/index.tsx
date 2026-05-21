import React from 'react';
import { Plus, Power, Settings } from 'lucide-react';
import { MOCK_AUDIT_LOGS } from '../../lib/constants';

const ACTION_ICONS: Record<string, React.ElementType> = {
  Created: Plus,
  Activated: Power,
  Updated: Settings,
};

export const AuditPage: React.FC = () => (
  <div style={{ maxWidth: 960, margin: '0 auto' }}>
    <div style={{ marginBottom: 32 }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, margin: '0 0 4px 0' }}>Audit Logs</h1>
      <p style={{ fontSize: 13, color: 'var(--color-text-secondary)', margin: 0 }}>
        Lịch sử thay đổi cấu hình. Ai đã tạo, sửa, kích hoạt integration nào và khi nào.
      </p>
    </div>

    <div className="card" style={{ overflow: 'hidden' }}>
      {MOCK_AUDIT_LOGS.map(log => {
        const Icon = ACTION_ICONS[log.action] ?? Settings;
        return (
          <div key={log.id} style={{
            display: 'flex', alignItems: 'center', gap: 14,
            padding: '14px 20px', borderBottom: '1px solid var(--color-border)',
          }}>
            <div style={{
              width: 32, height: 32, borderRadius: 8,
              background: 'var(--color-bg-subtle)', border: '1px solid var(--color-border)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}>
              <Icon size={14} color="var(--color-text-secondary)" />
            </div>
            <div style={{ flex: 1 }}>
              <div style={{ fontSize: 13 }}>
                <strong>{log.action}</strong>{' '}
                <code style={{ fontFamily: 'var(--font-mono)', fontSize: 12, color: 'var(--color-brand)' }}>{log.entity}</code>
              </div>
              <div style={{ fontSize: 11, color: 'var(--color-text-tertiary)', marginTop: 2 }}>
                bởi {log.user}
              </div>
            </div>
            <span style={{ fontSize: 11, color: 'var(--color-text-tertiary)', fontFamily: 'var(--font-mono)' }}>
              {log.time}
            </span>
          </div>
        );
      })}
    </div>
  </div>
);
