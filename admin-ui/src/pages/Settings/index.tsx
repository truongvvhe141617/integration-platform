import React, { useState } from 'react';
import { Save, Eye, EyeOff, Shield, Globe, Clock } from 'lucide-react';
import { toast } from '../../lib/toast';
import { MSG } from '../../lib/constants';

export const SettingsPage: React.FC = () => {
  const [apiKey, setApiKey] = useState('');
  const [showKey, setShowKey] = useState(false);
  const [gatewayUrl, setGatewayUrl] = useState('http://localhost:5000');
  const [defaultTimeout, setDefaultTimeout] = useState('30000');
  const [saved, setSaved] = useState(false);

  const handleSave = () => {
    if (apiKey) localStorage.setItem('api_secret_key', apiKey);
    localStorage.setItem('gateway_url', gatewayUrl);
    localStorage.setItem('default_timeout', defaultTimeout);
    toast.success(MSG.SETTINGS_SAVED);
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  return (
    <div style={{ maxWidth: 640, margin: '0 auto' }}>
      <div style={{ marginBottom: 32 }}>
        <h1 style={{ fontSize: 24, fontWeight: 700, margin: '0 0 4px 0' }}>Settings</h1>
        <p style={{ fontSize: 13, color: 'var(--color-text-secondary)', margin: 0 }}>
          Cấu hình chung cho platform. API key dùng để mã hóa thông tin xác thực của đối tác.
        </p>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
        {/* API Secret Key */}
        <div className="card" style={{ padding: 20 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
            <Shield size={16} color="var(--color-brand)" />
            <span style={{ fontSize: 14, fontWeight: 600 }}>API Secret Key</span>
          </div>
          <p style={{ fontSize: 12, color: 'var(--color-text-tertiary)', margin: '0 0 12px 0' }}>
            Key này dùng để mã hóa auth credentials (API key, token) của các đối tác khi lưu vào DB.
            Đặt trong biến môi trường <code style={{ fontFamily: 'var(--font-mono)', color: 'var(--color-brand)' }}>ENCRYPTION_SECRET</code> ở production.
          </p>
          <div style={{ display: 'flex', gap: 8 }}>
            <input
              className="input"
              type={showKey ? 'text' : 'password'}
              value={apiKey}
              onChange={e => setApiKey(e.target.value)}
              placeholder="sk-xxxxxxxx-xxxx-xxxx"
              style={{ flex: 1, fontFamily: 'var(--font-mono)', fontSize: 12 }}
            />
            <button className="btn btn-ghost btn-md" onClick={() => setShowKey(!showKey)}>
              {showKey ? <EyeOff size={14} /> : <Eye size={14} />}
            </button>
          </div>
        </div>

        {/* Gateway URL */}
        <div className="card" style={{ padding: 20 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
            <Globe size={16} color="var(--color-brand)" />
            <span style={{ fontSize: 14, fontWeight: 600 }}>Gateway URL</span>
          </div>
          <p style={{ fontSize: 12, color: 'var(--color-text-tertiary)', margin: '0 0 12px 0' }}>
            URL của API Gateway. Frontend gọi tất cả API qua gateway này.
          </p>
          <input
            className="input"
            value={gatewayUrl}
            onChange={e => setGatewayUrl(e.target.value)}
            placeholder="http://localhost:5000"
            style={{ fontFamily: 'var(--font-mono)', fontSize: 12 }}
          />
        </div>

        {/* Default Timeout */}
        <div className="card" style={{ padding: 20 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 16 }}>
            <Clock size={16} color="var(--color-brand)" />
            <span style={{ fontSize: 14, fontWeight: 600 }}>Default Timeout</span>
          </div>
          <p style={{ fontSize: 12, color: 'var(--color-text-tertiary)', margin: '0 0 12px 0' }}>
            Timeout mặc định (ms) khi tạo integration mới.
          </p>
          <input
            className="input"
            type="number"
            value={defaultTimeout}
            onChange={e => setDefaultTimeout(e.target.value)}
            style={{ width: 140 }}
          />
        </div>

        {/* Save */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <button className="btn btn-primary btn-md" onClick={handleSave}>
            <Save size={14} /> Lưu cài đặt
          </button>
          {saved && (
            <span style={{ fontSize: 12, color: 'var(--color-success)', fontWeight: 500 }}>
              ✓ Đã lưu
            </span>
          )}
        </div>
      </div>
    </div>
  );
};
