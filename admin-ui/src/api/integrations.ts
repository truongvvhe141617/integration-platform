import { apiClient } from './client';
import { ENDPOINTS } from './endpoints';

// ── Types ────────────────────────────────────────────────────

export interface MappingDto {
  sourceField: string;
  targetField: string;
  defaultValue?: string;
  transform?: string;
  isRequired?: boolean;
  sortOrder?: number;
}

export interface ValidationDto {
  field: string;
  rule: string;
  errorMessage?: string;
}

export interface OperationDto {
  id?: string;
  name: string;
  httpMethod: string;
  path: string;
  contentType?: string;
  description?: string;
  isEnabled?: boolean;
  requestMappings: MappingDto[];
  responseMappings: MappingDto[];
  validations: ValidationDto[];
}

export interface IntegrationConfig {
  id: string;
  configKey: string;
  name: string;
  description?: string;
  connectorType: string;
  status: 'draft' | 'active' | 'inactive' | 'archived';
  version: number;
  tags?: string;
  baseUrl: string;
  defaultHeaders?: string;
  authType: string;
  authParams?: string;
  timeoutMs: number;
  retryConfig?: string;
  circuitBreaker?: string;
  metadata?: string;
  createdBy: string;
  updatedBy?: string;
  createdDate?: string;
  updatedDate?: string;
  operations: OperationDto[];
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface TestResult {
  success: boolean;
  requestSent: object;
  responseReceived: object;
  mappedResponse: object;
  executionTimeMs: number;
}

// ── API calls ────────────────────────────────────────────────

export const integrationsApi = {
  getAll: (params?: { search?: string; status?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<IntegrationConfig>>(ENDPOINTS.integrations.list, { params }),

  getById: (id: string) =>
    apiClient.get<IntegrationConfig>(ENDPOINTS.integrations.getById(id)),

  getByKey: (key: string) =>
    apiClient.get<IntegrationConfig>(ENDPOINTS.integrations.getByKey(key)),

  create: (data: Partial<IntegrationConfig>) =>
    apiClient.post<IntegrationConfig>(ENDPOINTS.integrations.create, data),

  update: (id: string, data: Partial<IntegrationConfig>) =>
    apiClient.put<IntegrationConfig>(ENDPOINTS.integrations.update(id), data),

  delete: (id: string) =>
    apiClient.delete(ENDPOINTS.integrations.delete(id)),

  changeStatus: (id: string, status: string) =>
    apiClient.patch(ENDPOINTS.integrations.status(id), { status }),

  test: (id: string, operation: string, testData: Record<string, unknown>) =>
    apiClient.post<TestResult>(ENDPOINTS.integrations.test(id), { operation, testData }),

  getHistory: (id: string) =>
    apiClient.get(ENDPOINTS.integrations.history(id)),

  getAuditLog: (id: string) =>
    apiClient.get(ENDPOINTS.integrations.audit(id)),
};
