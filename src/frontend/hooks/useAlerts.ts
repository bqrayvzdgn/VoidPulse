'use client';

import { useEffect, useRef, useState, useCallback, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { api } from '@/lib/api';
import { getAccessToken } from '@/lib/auth';
import { API_BASE } from '@/lib/constants';
import type {
  AlertRuleResponse,
  AlertResponse,
  AlertCountResponse,
  CreateAlertRuleRequest,
  UpdateAlertRuleRequest,
} from '@/types/alerts';
import type { PagedResult } from '@/types/api';

// --- REST hooks ---

export function useAlertRules() {
  return useQuery({
    queryKey: ['alerts', 'rules'],
    queryFn: () => api.get<AlertRuleResponse[]>('/alerts/rules'),
    select: (res) => res.data,
  });
}

export function useCreateAlertRule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateAlertRuleRequest) =>
      api.post<AlertRuleResponse>('/alerts/rules', data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts', 'rules'] }),
  });
}

export function useUpdateAlertRule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ ruleId, data }: { ruleId: string; data: UpdateAlertRuleRequest }) =>
      api.put<AlertRuleResponse>(`/alerts/rules/${ruleId}`, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts', 'rules'] }),
  });
}

export function useDeleteAlertRule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ruleId: string) => api.delete<void>(`/alerts/rules/${ruleId}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['alerts', 'rules'] }),
  });
}

export function useAlerts(
  isAcknowledged?: boolean | null,
  severity?: number | null,
  page = 1,
  pageSize = 20
) {
  const params = new URLSearchParams();
  if (isAcknowledged !== null && isAcknowledged !== undefined)
    params.set('isAcknowledged', String(isAcknowledged));
  if (severity !== null && severity !== undefined)
    params.set('severity', String(severity));
  params.set('page', String(page));
  params.set('pageSize', String(pageSize));

  return useQuery({
    queryKey: ['alerts', 'list', isAcknowledged, severity, page, pageSize],
    queryFn: () => api.get<PagedResult<AlertResponse>>(`/alerts?${params.toString()}`),
    select: (res) => res.data,
    refetchInterval: 30000,
  });
}

export function useUnacknowledgedCount() {
  return useQuery({
    queryKey: ['alerts', 'count'],
    queryFn: () => api.get<AlertCountResponse>('/alerts/count'),
    select: (res) => res.data,
    refetchInterval: 15000,
  });
}

export function useAcknowledgeAlert() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (alertId: string) =>
      api.post<void>(`/alerts/${alertId}/acknowledge`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['alerts', 'list'] });
      queryClient.invalidateQueries({ queryKey: ['alerts', 'count'] });
    },
  });
}

// --- SignalR hook for real-time alerts ---

function getTenantIdFromToken(): string | null {
  const token = getAccessToken();
  if (!token) return null;
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.tenant_id || null;
  } catch {
    return null;
  }
}

export function useAlertHub() {
  const [recentAlerts, setRecentAlerts] = useState<AlertResponse[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const queryClient = useQueryClient();

  const tenantId = useMemo(() => getTenantIdFromToken(), []);

  const clearAlerts = useCallback(() => {
    setRecentAlerts([]);
    setUnreadCount(0);
  }, []);

  useEffect(() => {
    if (!tenantId) return;

    const hubUrl = API_BASE.replace('/api/v1', '') + '/hubs/traffic';

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => getAccessToken() || '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('AlertTriggered', (alert: AlertResponse) => {
      setRecentAlerts(prev => [alert, ...prev].slice(0, 50));
      setUnreadCount(prev => prev + 1);
      queryClient.invalidateQueries({ queryKey: ['alerts', 'count'] });
      queryClient.invalidateQueries({ queryKey: ['alerts', 'list'] });
    });

    connection.onreconnected(() => {
      connection.invoke('JoinTenant', tenantId).catch(() => {});
    });

    connection
      .start()
      .then(() => connection.invoke('JoinTenant', tenantId))
      .catch(() => {});

    return () => {
      connection.stop();
    };
  }, [tenantId, queryClient]);

  return { recentAlerts, unreadCount, clearAlerts };
}
