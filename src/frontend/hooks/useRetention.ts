'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import type { RetentionPolicyResponse, RetentionPolicyRequest } from '@/types/retention';

export function useRetentionPolicy() {
  return useQuery({
    queryKey: ['retention'],
    queryFn: () => api.get<RetentionPolicyResponse>('/retention'),
    select: (res) => res.data,
  });
}

export function useUpdateRetentionPolicy() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: RetentionPolicyRequest) => api.put<RetentionPolicyResponse>('/retention', data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['retention'] }),
  });
}
