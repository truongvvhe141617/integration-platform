import React from 'react';
import { Info } from 'lucide-react';

interface FormFieldProps {
  label: string;
  required?: boolean;
  hint?: string;
  error?: string;
  children: React.ReactNode;
}

export const FormField: React.FC<FormFieldProps> = ({ label, required, hint, error, children }) => (
  <div className="form-group">
    <label className="form-label">
      {label}
      {required && <span style={{ color: 'var(--color-danger)', fontSize: 10 }}>*</span>}
    </label>
    {children}
    {error && (
      <span style={{ fontSize: 'var(--text-xs)', color: 'var(--color-danger)', marginTop: 4 }}>
        {error}
      </span>
    )}
    {hint && !error && (
      <span className="form-hint">
        <Info size={11} style={{ marginTop: 2, flexShrink: 0 }} />
        <span>{hint}</span>
      </span>
    )}
  </div>
);
