import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { integrationsApi, IntegrationConfig } from '../api/integrations';
import { toast } from '../lib/toast';
import { MSG } from '../lib/constants';

const QUERY_KEY = 'integrations';

export const useIntegrations = (params?: {
  search?: string; status?: string; tags?: string;
  page?: number; pageSize?: number;
}) => useQuery({
  queryKey: [QUERY_KEY, params],
  queryFn: async () => {
    const res = await integrationsApi.getAll(params);
    return res.data;
  },
  retry: false,
});

export const useIntegration = (id: string) => useQuery({
  queryKey: [QUERY_KEY, id],
  queryFn: async () => {
    const res = await integrationsApi.getById(id);
    return res.data;
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
      toast.success(MSG.INTEGRATION_CREATED);
    },
    onError: () => toast.error(MSG.ERR_CREATE_INTEGRATION),
  });
};

export const useUpdateIntegration = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<IntegrationConfig> }) =>
      integrationsApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [QUERY_KEY] });
      toast.success(MSG.INTEGRATION_UPDATED);
    },
    onError: () => toast.error(MSG.ERR_UPDATE_INTEGRATION),
  });
};

export const useDeleteIntegration = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => integrationsApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [QUERY_KEY] });
      toast.success(MSG.INTEGRATION_DELETED);
    },
    onError: () => toast.error(MSG.ERR_DELETE_INTEGRATION),
  });
};

export const useChangeStatus = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      integrationsApi.changeStatus(id, status),
    onSuccess: (_, { status }) => {
      qc.invalidateQueries({ queryKey: [QUERY_KEY] });
      toast.success(status === 'active' ? MSG.INTEGRATION_ACTIVATED : MSG.INTEGRATION_DEACTIVATED);
    },
  });
};

export const useTestIntegration = () => useMutation({
  mutationFn: ({ id, operation, testData }: {
    id: string; operation: string; testData: Record<string, unknown>;
  }) => integrationsApi.test(id, operation, testData).then(r => r.data),
});
