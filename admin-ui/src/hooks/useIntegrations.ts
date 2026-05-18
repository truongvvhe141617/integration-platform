import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { integrationsApi, IntegrationConfig, PagedResult } from '../api/integrations';
import { message } from 'antd';

const QUERY_KEY = 'integrations';

// Mock data khi API chưa chạy (dev mode)
const MOCK_DATA: PagedResult<IntegrationConfig> = {
  items: [
    {
      id: 'mock-1', configKey: 'bank-b-transfer', name: 'Bank B - Fund Transfer',
      connectorType: 'generic-http', status: 'active', version: 1,
      baseUrl: 'http://localhost:5002/mock', authType: 'apikey',
      timeoutMs: 30000, createdBy: 'system', operations: [
        { name: 'transfer', httpMethod: 'POST', path: '/bank-b/v1/fund-transfer', requestMappings: [], responseMappings: [], validations: [] },
        { name: 'balance-inquiry', httpMethod: 'POST', path: '/bank-b/v1/balance', requestMappings: [], responseMappings: [], validations: [] },
      ],
    },
    {
      id: 'mock-2', configKey: 'bank-c-transfer', name: 'Bank C - Fund Transfer',
      connectorType: 'generic-http', status: 'active', version: 1,
      baseUrl: 'http://localhost:5002/mock', authType: 'bearer',
      timeoutMs: 20000, createdBy: 'system', operations: [
        { name: 'transfer', httpMethod: 'POST', path: '/bank-c/api/transfer', requestMappings: [], responseMappings: [], validations: [] },
      ],
    },
    {
      id: 'mock-3', configKey: 'ewallet-payment', name: 'E-Wallet Payment',
      connectorType: 'generic-http', status: 'active', version: 1,
      baseUrl: 'http://localhost:5002/mock', authType: 'apikey',
      timeoutMs: 15000, createdBy: 'system', operations: [
        { name: 'payment', httpMethod: 'POST', path: '/ewallet/api/v2/payment', requestMappings: [], responseMappings: [], validations: [] },
      ],
    },
    {
      id: 'mock-4', configKey: 'sms-gateway', name: 'SMS Gateway - OTP',
      connectorType: 'generic-http', status: 'draft', version: 1,
      baseUrl: 'http://localhost:5002/mock', authType: 'apikey',
      timeoutMs: 10000, createdBy: 'system', operations: [
        { name: 'send-otp', httpMethod: 'POST', path: '/sms/api/send', requestMappings: [], responseMappings: [], validations: [] },
      ],
    },
    {
      id: 'mock-5', configKey: 'bank-e-transfer', name: 'Bank E - Custom Connector',
      connectorType: 'bank-e', status: 'inactive', version: 2,
      baseUrl: 'http://localhost:5002/mock', authType: 'custom',
      timeoutMs: 30000, createdBy: 'system', operations: [
        { name: 'transfer', httpMethod: 'POST', path: '/bank-e/partner/v1/transfer', requestMappings: [], responseMappings: [], validations: [] },
      ],
    },
  ],
  total: 5, page: 1, pageSize: 20,
};

export const useIntegrations = (params?: {
  search?: string; status?: string; tags?: string;
  page?: number; pageSize?: number;
}) => useQuery({
  queryKey: [QUERY_KEY, params],
  queryFn: async () => {
    try {
      const res = await integrationsApi.getAll(params);
      return res.data;
    } catch {
      // Fallback to mock data when API is not available (dev mode)
      console.warn('[useIntegrations] API unavailable, using mock data');
      let items = MOCK_DATA.items;
      if (params?.search) {
        const q = params.search.toLowerCase();
        items = items.filter(i => i.name.toLowerCase().includes(q) || i.configKey.toLowerCase().includes(q));
      }
      if (params?.status) {
        items = items.filter(i => i.status === params.status);
      }
      return { ...MOCK_DATA, items, total: items.length };
    }
  },
  retry: false,
});

export const useIntegration = (id: string) => useQuery({
  queryKey: [QUERY_KEY, id],
  queryFn: async () => {
    try {
      const res = await integrationsApi.getById(id);
      return res.data;
    } catch {
      return MOCK_DATA.items.find(i => i.id === id) ?? null;
    }
  },
  enabled: !!id,
  retry: false,
});

export const useCreateIntegration = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<IntegrationConfig>) => integrationsApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [QUERY_KEY] });
      message.success('Integration created successfully');
    },
    onError: () => message.error('Failed to create integration'),
  });
};

export const useUpdateIntegration = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<IntegrationConfig> }) =>
      integrationsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [QUERY_KEY] });
      message.success('Integration updated successfully');
    },
  });
};

export const useDeleteIntegration = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => integrationsApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [QUERY_KEY] });
      message.success('Integration deleted');
    },
  });
};

export const useChangeStatus = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      integrationsApi.changeStatus(id, status),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
};

export const useTestIntegration = () => useMutation({
  mutationFn: ({ id, operation, testData }: {
    id: string; operation: string; testData: Record<string, unknown>;
  }) => integrationsApi.test(id, operation, testData).then(r => r.data),
});
