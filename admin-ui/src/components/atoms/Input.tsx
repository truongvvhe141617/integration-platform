import React, { forwardRef } from 'react';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  mono?: boolean;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ mono, style, ...props }, ref) => (
    <input
      ref={ref}
      className="input"
      style={{
        ...(mono ? { fontFamily: 'var(--font-mono)', fontSize: 'var(--text-sm)' } : {}),
        ...style,
      }}
      {...props}
    />
  )
);

Input.displayName = 'Input';

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ children, ...props }, ref) => (
    <select ref={ref} className="input" {...props}>
      {children}
    </select>
  )
);

Select.displayName = 'Select';
