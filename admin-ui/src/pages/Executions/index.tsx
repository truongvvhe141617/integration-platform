import React from 'react';
import { Zap, Clock, CheckCircle, XCircle } from 'lucide-react';
import { MOCK_EXECUTIONS } from '../../lib/constants';

export const ExecutionsPage: React.FC = () => (
  <div style={{ maxWidth: 960, margin: '0 auto' }}>
    <div style={{ marginBottom: 32 }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, margin: '0 0 4px 0' }}>Executions</h1>
      <p style={{ fontSize: 13, color: 'var(--color-text-secondary)', margin: 0 }}>
        Lịch sử các lần gọi API tới đối tác. Mỗi request qua Integration Service đều được ghi log.
      </p>
    </div>

    {/* Stats */}
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16, marginBottom: 24 }}>
      <div className="card" style={{ padding: '14px 20px', display: 'flex', alignItems: 'center', gap: 12 }}>
        <Zap size={16} color="var(--color-brand)" />
        <div>
          <div style={{ fontSize: 18, fontWeight: 700 }}>127</div>
          <div style={{ fontSize: 11, color: 'var(--color-text-secondary)' }}>Hôm nay</div>
        </div>
      </div>
      <div className="card" style={{ padding: '14px 20px', display: 'flex', alignItems: 'center', gap: 12 }}>
        <CheckCircle size={16} color="var(--color-success)" />
        <div>
          <div style={{ fontSize: 18, fontWeight: 700 }}>98.4%</div>
          <div style={{ fontSize: 11, color: 'var(--color-text-secondary)' }}>Tỷ lệ thành công</div>
        </div>
      </div>
      <div className="card" style={{ padding: '14px 20px', display: 'flex', alignItems: 'center', gap: 12 }}>
        <Clock size={16} color="var(--color-warning)" />
        <div>
          <div style={{ fontSize: 18, fontWeight: 700 }}>186ms</div>
          <div style={{ fontSize: 11, color: 'var(--color-text-secondary)' }}>Avg latency</div>
        </div>
      </div>
    </div>

    {/* Table */}
    <div className="card" style={{ overflow: 'hidden' }}>
      <div style={{
        display: 'grid', gridTemplateColumns: '1fr 1fr 80px 80px 100px',
        padding: '10px 20px', background: 'var(--color-bg-subtle)',
        borderBottom: '1px solid var(--color-border)',
        fontSize: 11, fontWeight: 600, color: 'var(--color-text-secondary)',
        textTransform: 'uppercase', letterSpacing: '0.04em',
      }}>
        <span>Config</span><span>Operation</span><span>Status</span><span>Duration</span><span>Time</span>
      </div>
      {MOCK_EXECUTIONS.map(exec => (
        <div key={exec.id} style={{
          display: 'grid', gridTemplateColumns: '1fr 1fr 80px 80px 100px',
          padding: '12px 20px', borderBottom: '1px solid var(--color-border)',
          fontSize: 13, alignItems: 'center',
        }}>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 12 }}>{exec.configKey}</span>
          <span style={{ color: 'var(--color-text-secondary)' }}>{exec.operation}</span>
          <span>
            {exec.status === 'success'
              ? <span style={{ display: 'flex', alignItems: 'center', gap: 4, color: 'var(--color-success)', fontSize: 11, fontWeight: 600 }}><CheckCircle size={12} /> OK</span>
              : <span style={{ display: 'flex', alignItems: 'center', gap: 4, color: 'var(--color-danger)', fontSize: 11, fontWeight: 600 }}><XCircle size={12} /> FAIL</span>
            }
          </span>
          <span style={{ fontFamily: 'var(--font-mono)', fontSize: 11, color: 'var(--color-text-secondary)' }}>{exec.duration}ms</span>
          <span style={{ fontSize: 11, color: 'var(--color-text-tertiary)' }}>{exec.time}</span>
        </div>
      ))}
    </div>
  </div>
);
