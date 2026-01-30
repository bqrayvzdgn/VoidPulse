'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import type { AgentKeyResponse, CreateAgentKeyRequest, UpdateAgentKeyRequest } from '@/types/agent';

export function useAgentKeys() {
  return useQuery({
    queryKey: ['agents'],
    queryFn: () => api.get<AgentKeyResponse[]>('/agents'),
    select: (res) => res.data,
  });
}

export function useCreateAgentKey() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateAgentKeyRequest) => api.post<AgentKeyResponse>('/agents', data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['agents'] }),
  });
}

export function useUpdateAgentKey(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateAgentKeyRequest) => api.put<AgentKeyResponse>(`/agents/${id}`, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['agents'] }),
  });
}

export function useDeleteAgentKey() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<boolean>(`/agents/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['agents'] }),
  });
}
