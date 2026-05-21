import React from 'react';
import { Loader2 } from 'lucide-react';

interface SpinnerProps {
  size?: number;
  color?: string;
}

export const Spinner: React.FC<SpinnerProps> = ({ size = 16, color = 'var(--color-brand)' }) => (
  <Loader2 size={size} color={color} className="animate-spin" />
);

export const PageSpinner: React.FC = () => (
  <div style={{
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    height: 256, width: '100%',
  }}>
    <Spinner size={20} />
  </div>
);
