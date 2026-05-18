import axios, { AxiosError, AxiosResponse } from 'axios';

/**
 * Axios instance — dùng chung cho tất cả API calls.
 * Base URL không set ở đây vì mỗi service có URL riêng (xem endpoints.ts).
 */
export const apiClient = axios.create({
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
});

// ── Request interceptor ──────────────────────────────────────
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('access_token');
  const tenantId = localStorage.getItem('tenant_id') || 'default';

  if (token) config.headers.Authorization = `Bearer ${token}`;
  config.headers['X-Tenant-Id'] = tenantId;
  config.headers['X-Correlation-Id'] = crypto.randomUUID();

  return config;
});

// ── Response interceptor ─────────────────────────────────────
apiClient.interceptors.response.use(
  (res: AxiosResponse) => res,
  (err: AxiosError) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('access_token');
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);
