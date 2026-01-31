'use client';

import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { buildQueryString } from '@/lib/utils';
import type { PagedResult } from '@/types/api';
import type { CapturedPacketResponse, PacketQueryParams } from '@/types/traffic';

export function useFlowPackets(flowId: string, params: { page: number; pageSize: number }) {
  return useQuery({
    queryKey: ['packets', 'flow', flowId, params],
    queryFn: () => api.get<PagedResult<CapturedPacketResponse>>(`/traffic/${flowId}/packets${buildQueryString(params as Record<string, string | number | undefined>)}`),
    select: (res) => res.data,
    enabled: !!flowId,
  });
}

export function usePackets(params: PacketQueryParams) {
  return useQuery({
    queryKey: ['packets', params],
    queryFn: () => api.get<PagedResult<CapturedPacketResponse>>(`/traffic/packets${buildQueryString(params as Record<string, string | number | undefined>)}`),
    select: (res) => res.data,
  });
}

export function usePacket(id: string) {
  return useQuery({
    queryKey: ['packets', id],
    queryFn: () => api.get<CapturedPacketResponse>(`/traffic/packets/${id}`),
    select: (res) => res.data,
    enabled: !!id,
  });
}
