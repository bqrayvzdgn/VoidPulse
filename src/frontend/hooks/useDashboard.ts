'use client';

import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import type { OverviewResponse, TopTalkersResponse, ProtocolDistributionResponse, BandwidthResponse, SitesResponse, ProcessesResponse } from '@/types/dashboard';

export function useOverview(period = '24h') {
  return useQuery({
    queryKey: ['dashboard', 'overview', period],
    queryFn: () => api.get<OverviewResponse>(`/dashboard/overview?period=${period}`),
    select: (res) => res.data,
  });
}

export function useTopTalkers(period = '24h', limit = 10) {
  return useQuery({
    queryKey: ['dashboard', 'top-talkers', period, limit],
    queryFn: () => api.get<TopTalkersResponse>(`/dashboard/top-talkers?period=${period}&limit=${limit}`),
    select: (res) => res.data,
  });
}

export function useProtocolDistribution(period = '24h') {
  return useQuery({
    queryKey: ['dashboard', 'protocol-distribution', period],
    queryFn: () => api.get<ProtocolDistributionResponse>(`/dashboard/protocol-distribution?period=${period}`),
    select: (res) => res.data,
  });
}

export function useBandwidth(period = '24h') {
  return useQuery({
    queryKey: ['dashboard', 'bandwidth', period],
    queryFn: () => api.get<BandwidthResponse>(`/dashboard/bandwidth?period=${period}`),
    select: (res) => res.data,
  });
}

export function useTopSites(period = '24h', limit = 20) {
  return useQuery({
    queryKey: ['dashboard', 'sites', period, limit],
    queryFn: () => api.get<SitesResponse>(`/dashboard/sites?period=${period}&limit=${limit}`),
    select: (res) => res.data,
  });
}

export function useTopProcesses(period = '24h', limit = 20) {
  return useQuery({
    queryKey: ['dashboard', 'processes', period, limit],
    queryFn: () => api.get<ProcessesResponse>(`/dashboard/processes?period=${period}&limit=${limit}`),
    select: (res) => res.data,
  });
}
