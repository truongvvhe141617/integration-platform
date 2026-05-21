import React from 'react';

type TextVariant = 'h1' | 'h2' | 'h3' | 'body' | 'caption' | 'mono';

const STYLES: Record<TextVariant, React.CSSProperties> = {
  h1: { fontSize: 'var(--text-3xl)', fontWeight: 700, color: 'var(--color-text-primary)', lineHeight: 1.2 },
  h2: { fontSize: 'var(--text-xl)', fontWeight: 600, color: 'var(--color-text-primary)', lineHeight: 1.3 },
  h3: { fontSize: 'var(--text-md)', fontWeight: 600, color: 'var(--color-text-primary)', lineHeight: 1.4 },
  body: { fontSize: 'var(--text-sm)', color: 'var(--color-text-secondary)', lineHeight: 1.5 },
  caption: { fontSize: 'var(--text-xs)', color: 'var(--color-text-tertiary)', lineHeight: 1.4 },
  mono: { fontSize: 'var(--text-sm)', fontFamily: 'var(--font-mono)', color: 'var(--color-text-primary)' },
};

interface TextProps {
  variant?: TextVariant;
  children: React.ReactNode;
  style?: React.CSSProperties;
  as?: keyof JSX.IntrinsicElements;
}

export const Text: React.FC<TextProps> = ({ variant = 'body', children, style, as: Tag = 'span' }) => (
  <Tag style={{ margin: 0, ...STYLES[variant], ...style }}>{children}</Tag>
);
