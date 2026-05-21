import React from 'react';
import { Search, X } from 'lucide-react';

interface SearchBarProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

export const SearchBar: React.FC<SearchBarProps> = ({ value, onChange, placeholder = 'Search...' }) => (
  <div style={{ position: 'relative', width: '100%', maxWidth: 280 }}>
    <Search
      size={14}
      style={{
        position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)',
        color: 'var(--color-text-tertiary)', pointerEvents: 'none',
      }}
    />
    <input
      className="input"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      style={{ paddingLeft: 32, paddingRight: value ? 32 : 12 }}
    />
    {value && (
      <button
        onClick={() => onChange('')}
        style={{
          position: 'absolute', right: 8, top: '50%', transform: 'translateY(-50%)',
          width: 18, height: 18, borderRadius: '50%', border: 'none',
          background: 'var(--color-bg-hover)', color: 'var(--color-text-tertiary)',
          cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
        }}
      >
        <X size={10} />
      </button>
    )}
  </div>
);
