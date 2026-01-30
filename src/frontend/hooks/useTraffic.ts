'use client';

import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { buildQueryString } from '@/lib/utils';
import { API_BASE } from '@/lib/constants';
import type { PagedResult } from '@/types/api';
import type { TrafficFlowResponse, TrafficQueryParams } from '@/types/traffic';

export function useTrafficFlows(params: TrafficQueryParams) {
  return useQuery({
    queryKey: ['traffic', params],
    queryFn: () => api.get<PagedResult<TrafficFlowResponse>>(`/traffic${buildQueryString(params as Record<string, string | number | undefined>)}`),
    select: (res) => res.data,
  });
}

export function useTrafficFlow(id: string) {
  return useQuery({
    queryKey: ['traffic', id],
    queryFn: () => api.get<TrafficFlowResponse>(`/traffic/${id}`),
    select: (res) => res.data,
    enabled: !!id,
  });
}

export function exportTrafficCsv(params: TrafficQueryParams): void {
  const token = typeof window !== 'undefined' ? localStorage.getItem('voidpulse_access_token') : null;
  const qs = buildQueryString(params as Record<string, string | number | undefined>);
  const url = `${API_BASE}/traffic/export${qs}`;

  const link = document.createElement('a');
  link.href = url;
  link.setAttribute('download', 'traffic-export.csv');
  // For auth, we need to fetch with token and create blob
  fetch(url, {
    headers: { Authorization: `Bearer ${token}` },
  })
    .then(res => res.blob())
    .then(blob => {
      const blobUrl = URL.createObjectURL(blob);
      link.href = blobUrl;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(blobUrl);
    });
}
