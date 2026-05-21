import React, { Component, ErrorInfo, ReactNode, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AppLayout } from './components/templates/AppLayout';
import { IntegrationListPage } from './pages/IntegrationList';
import { IntegrationFormPage } from './pages/IntegrationForm';
import { IntegrationTestPage } from './pages/IntegrationTest';
import { DashboardPage } from './pages/Dashboard';
import { ExecutionsPage } from './pages/Executions';
import { AuditPage } from './pages/Audit';
import { SettingsPage } from './pages/Settings';

class ErrorBoundary extends Component<{ children: ReactNode }, { error: Error | null }> {
  state: { error: Error | null } = { error: null };
  static getDerivedStateFromError(e: Error) { return { error: e }; }
  componentDidCatch(e: Error, info: ErrorInfo) { console.error('[ErrorBoundary]', e, info); }
  render() {
    if (this.state.error) {
      return (
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100vh', gap: 16, padding: 24 }}>
          <div style={{ fontSize: 40 }}>⚠️</div>
          <h2 style={{ fontSize: 18, fontWeight: 600, color: 'var(--color-danger)' }}>Something went wrong</h2>
          <pre style={{ fontSize: 12, color: 'var(--color-text-secondary)', maxWidth: 500, overflow: 'auto', background: 'var(--color-bg-elevated)', padding: 16, borderRadius: 8 }}>
            {this.state.error.message}
          </pre>
          <button className="btn btn-primary btn-md" onClick={() => { this.setState({ error: null }); window.location.reload(); }}>
            Reload
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30_000, retry: false, refetchOnWindowFocus: false } },
});

const App: React.FC = () => (
  <ErrorBoundary>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Suspense fallback={<div />}>
          <Routes>
            <Route path="/" element={<AppLayout />}>
              <Route index element={<Navigate to="/dashboard" replace />} />
              <Route path="dashboard" element={<DashboardPage />} />
              <Route path="integrations" element={<IntegrationListPage />} />
              <Route path="integrations/new" element={<IntegrationFormPage />} />
              <Route path="integrations/:id" element={<IntegrationFormPage />} />
              <Route path="integrations/:id/edit" element={<IntegrationFormPage />} />
              <Route path="integrations/:id/test" element={<IntegrationTestPage />} />
              <Route path="executions" element={<ExecutionsPage />} />
              <Route path="audit" element={<AuditPage />} />
              <Route path="settings" element={<SettingsPage />} />
            </Route>
          </Routes>
        </Suspense>
      </BrowserRouter>
    </QueryClientProvider>
  </ErrorBoundary>
);

export default App;
