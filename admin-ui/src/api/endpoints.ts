/**
 * ╔══════════════════════════════════════════════════════════════╗
 * ║  API ENDPOINTS — Single source of truth                      ║
 * ║  FE gọi tất cả qua Gateway (port 5000)                      ║
 * ║  Gateway route xuống từng service                            ║
 * ╚══════════════════════════════════════════════════════════════╝
 */

// Tất cả request đi qua Gateway
const GATEWAY = import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5000';

export const ENDPOINTS = {
  // ── Config Service (qua Gateway) ────────────────────────────
  integrations: {
    list:       `${GATEWAY}/api/v1/integrations`,
    create:     `${GATEWAY}/api/v1/integrations`,
    getById:    (id: string) => `${GATEWAY}/api/v1/integrations/${id}`,
    getByKey:   (key: string) => `${GATEWAY}/api/v1/integrations/by-key/${key}`,
    update:     (id: string) => `${GATEWAY}/api/v1/integrations/${id}`,
    delete:     (id: string) => `${GATEWAY}/api/v1/integrations/${id}`,
    status:     (id: string) => `${GATEWAY}/api/v1/integrations/${id}/status`,
    test:       (id: string) => `${GATEWAY}/api/v1/integrations/${id}/test`,
    history:    (id: string) => `${GATEWAY}/api/v1/integrations/${id}/history`,
    audit:      (id: string) => `${GATEWAY}/api/v1/integrations/${id}/audit`,
    duplicate:  (id: string) => `${GATEWAY}/api/v1/integrations/${id}/duplicate`,
  },

  // ── Integration Service (qua Gateway) ───────────────────────
  execute: {
    sync:       `${GATEWAY}/api/v1/integration/execute`,
    async:      `${GATEWAY}/api/v1/integration/execute-async`,
    health:     (configId: string) => `${GATEWAY}/api/v1/integration/health/${configId}`,
  },

  // ── Business Service (qua Gateway) ──────────────────────────
  payment: {
    process:    `${GATEWAY}/api/v1/business/payment`,
  },

  // ── Health Checks ────────────────────────────────────────────
  health: {
    gateway:    `${GATEWAY}/health`,
  },
} as const;
