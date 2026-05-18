import React, { Component, ErrorInfo, ReactNode, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AppLayout } from './components/Layout/AppLayout';
import { IntegrationListPage } from './pages/IntegrationList';
import { IntegrationFormPage } from './pages/IntegrationForm';
import { IntegrationTestPage } from './pages/IntegrationTest';

// ── Error Boundary ──
class ErrorBoundary extends Component<{ children: ReactNode }, { error: Error | null }> {
  state = { error: null };
  static getDerivedStateFromError(e: Error) { return { error: e }; }
  componentDidCatch(e: Error, info: ErrorInfo) { console.error('[ErrorBoundary]', e, info); }

  render() {
    if (this.state.error) {
      return (
        <div className="min-h-screen bg-bg-base flex flex-col items-center justify-center gap-4 p-6 font-mono">
          <div className="text-4xl">⚠️</div>
          <h2 className="text-xl font-semibold text-danger">Render Error</h2>
          <pre className="bg-bg-elevated border border-border rounded-lg p-4 text-xs text-warning max-w-xl overflow-auto">
            {(this.state.error as Error).message}
          </pre>
          <button
            onClick={() => { this.setState({ error: null }); window.location.reload(); }}
            className="btn-md btn-primary"
          >
            Reload
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}

// ── Loading fallback ──
const PageLoader = () => (
  <div className="flex items-center justify-center h-64">
    <div className="w-5 h-5 border-2 border-brand border-t-transparent rounded-full animate-spin" />
  </div>
);

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: false, refetchOnWindowFocus: false },
  },
});

const App: React.FC = () => (
  <ErrorBoundary>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Suspense fallback={<PageLoader />}>
          <Routes>
            <Route path="/" element={<AppLayout />}>
              <Route index element={<Navigate to="/integrations" replace />} />
              <Route path="integrations" element={<IntegrationListPage />} />
              <Route path="integrations/new" element={<IntegrationFormPage />} />
              <Route path="integrations/:id" element={<IntegrationFormPage />} />
              <Route path="integrations/:id/edit" element={<IntegrationFormPage />} />
              <Route path="integrations/:id/test" element={<IntegrationTestPage />} />
            </Route>
          </Routes>
        </Suspense>
      </BrowserRouter>
    </QueryClientProvider>
  </ErrorBoundary>
);

export default App;
