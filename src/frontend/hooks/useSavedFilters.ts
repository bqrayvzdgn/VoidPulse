'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import type { SavedFilterResponse, CreateSavedFilterRequest, UpdateSavedFilterRequest } from '@/types/savedFilter';

export function useSavedFilters() {
  return useQuery({
    queryKey: ['saved-filters'],
    queryFn: () => api.get<SavedFilterResponse[]>('/saved-filters'),
    select: (res) => res.data,
  });
}

export function useCreateSavedFilter() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateSavedFilterRequest) => api.post<SavedFilterResponse>('/saved-filters', data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['saved-filters'] }),
  });
}

export function useUpdateSavedFilter(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateSavedFilterRequest) => api.put<SavedFilterResponse>(`/saved-filters/${id}`, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['saved-filters'] }),
  });
}

export function useDeleteSavedFilter() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.delete<boolean>(`/saved-filters/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['saved-filters'] }),
  });
}
