'use client';

import { useEffect, useRef, useState, useCallback, useMemo } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { getAccessToken } from '@/lib/auth';
import { API_BASE } from '@/lib/constants';

export interface LiveTrafficFlow {
  id: string;
  sourceIp: string;
  destinationIp: string;
  sourcePort: number;
  destinationPort: number;
  protocol: string;
  bytesSent: number;
  bytesReceived: number;
  packetsSent: number;
  packetsReceived: number;
  startedAt: string;
  endedAt: string;
  flowDuration: number;
  processName?: string;
  resolvedHostname?: string;
  tlsSni?: string;
  createdAt: string;
}

const MAX_FLOWS = 100;

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

export function useTrafficHub() {
  const [recentFlows, setRecentFlows] = useState<LiveTrafficFlow[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [pongResult, setPongResult] = useState<string>('');
  const connectionRef = useRef<ReturnType<typeof HubConnectionBuilder.prototype.build> | null>(null);

  const tenantId = useMemo(() => getTenantIdFromToken(), []);

  const clearFlows = useCallback(() => {
    setRecentFlows([]);
  }, []);

  const sendPing = useCallback(() => {
    if (connectionRef.current) {
      connectionRef.current.invoke('Ping')
        .then((result: unknown) => setPongResult('Pong: ' + JSON.stringify(result)))
        .catch(err => setPongResult('Ping error: ' + String(err)));
    } else {
      setPongResult('No connection');
    }
  }, []);

  useEffect(() => {
    if (!tenantId) return;

    const hubUrl = API_BASE.replace('/api/v1', '') + '/hubs/traffic';

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => getAccessToken() || '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();

    connectionRef.current = connection;

    connection.on('Pong', (data: unknown) => {
      console.log('[SignalR] Pong received', data);
      setPongResult('Pong: ' + JSON.stringify(data));
    });

    connection.on('FlowIngested', (flow: LiveTrafficFlow) => {
      console.log('[SignalR] FlowIngested received', flow);
      setRecentFlows(prev => [flow, ...prev].slice(0, MAX_FLOWS));
    });

    connection.on('BatchIngested', (flows: LiveTrafficFlow[]) => {
      console.log('[SignalR] BatchIngested received', flows.length, 'flows');
      setRecentFlows(prev => [...flows.reverse(), ...prev].slice(0, MAX_FLOWS));
    });

    connection.onreconnected(() => {
      console.log('[SignalR] Reconnected');
      setIsConnected(true);
      connection.invoke('JoinTenant', tenantId).catch(err => console.error('[SignalR] JoinTenant failed on reconnect', err));
    });

    connection.onclose((err) => {
      console.log('[SignalR] Connection closed', err);
      setIsConnected(false);
    });

    console.log('[SignalR] Connecting to', hubUrl, 'tenant:', tenantId);
    connection
      .start()
      .then(() => {
        console.log('[SignalR] Connected, joining tenant', tenantId);
        setIsConnected(true);
        return connection.invoke('JoinTenant', tenantId);
      })
      .then(() => console.log('[SignalR] Joined tenant group'))
      .catch(err => {
        console.error('[SignalR] Connection failed', err);
        setIsConnected(false);
      });

    return () => {
      connection.stop();
      connectionRef.current = null;
    };
  }, [tenantId]);

  return { recentFlows, isConnected, clearFlows, flowCount: recentFlows.length, sendPing, pongResult };
}
