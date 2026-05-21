import React from 'react';

interface SkeletonProps {
  width?: number | string;
  height?: number | string;
  radius?: number | string;
  style?: React.CSSProperties;
}

export const Skeleton: React.FC<SkeletonProps> = ({ width, height = 14, radius = 4, style }) => (
  <div style={{
    width, height, borderRadius: radius,
    background: 'linear-gradient(90deg, var(--color-bg-subtle) 25%, var(--color-bg-hover) 50%, var(--color-bg-subtle) 75%)',
    backgroundSize: '200% 100%',
    animation: 'shimmer 1.5s ease-in-out infinite',
    ...style,
  }} />
);
