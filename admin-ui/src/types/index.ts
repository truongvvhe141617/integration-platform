export interface IntegrationConfig {
  id: string;
  configId: string;
  name: string;
  description?: string;
  connectorType: string;
  status: 'draft' | 'active' | 'inactive' | 'archived';
  version: number;
  baseUrl: string;
  defaultHeaders: Record<string, string>;
  authType: 'none' | 'apikey' | 'basic' | 'bearer' | 'oauth2';
  authParams: Record<string, string>;
  timeoutMs: number;
  retryConfig: RetryConfig;
  circuitBreakerConfig: CircuitBreakerConfig;
  tags: string[];
  operations: OperationConfig[];
  createdBy: string;
  updatedBy?: string;
  createdAt: string;
  updatedAt: string;
}

export interface OperationConfig {
  id?: string;
  name: string;
  httpMethod: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
  path: string;
  contentType?: string;
  description?: string;
  isEnabled: boolean;
  requestMappings: FieldMapping[];
  responseMappings: FieldMapping[];
  validations: ValidationRule[];
}

export interface FieldMapping {
  id?: string;
  sourceField: string;
  targetField: string;
  defaultValue?: string;
  transform?: string;
  isRequired?: boolean;
}

export interface ValidationRule {
  id?: string;
  field: string;
  rule: string;
  errorMessage?: string;
}

export interface RetryConfig {
  maxRetries: number;
  initialDelayMs: number;
  backoffStrategy: 'fixed' | 'linear' | 'exponential';
}

export interface CircuitBreakerConfig {
  failureThreshold: number;
  durationOfBreakSeconds: number;
  samplingDurationSeconds: number;
}

export interface TestResult {
  success: boolean;
  requestSent: { url: string; method: string; headers: Record<string, string>; body: unknown };
  responseReceived: { statusCode: number; headers: Record<string, string>; body: unknown };
  mappedResponse: Record<string, unknown>;
  executionTimeMs: number;
  error?: string;
}

export interface IntegrationListParams {
  status?: string;
  search?: string;
  tags?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}
