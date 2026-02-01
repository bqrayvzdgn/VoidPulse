'use client';

import { useEffect, useRef, useState, useCallback, useMemo } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { getAccessToken, getTenantIdFromToken } from '@/lib/auth';
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

    let cancelled = false;
    const hubUrl = API_BASE.replace('/api/v1', '') + '/hubs/traffic';

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => getAccessToken() || '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    connectionRef.current = connection;

    connection.on('Pong', (data: unknown) => {
      setPongResult('Pong: ' + JSON.stringify(data));
    });

    connection.on('FlowIngested', (flow: LiveTrafficFlow) => {
      setRecentFlows(prev => [flow, ...prev].slice(0, MAX_FLOWS));
    });

    connection.on('BatchIngested', (flows: LiveTrafficFlow[]) => {
      setRecentFlows(prev => [...flows.reverse(), ...prev].slice(0, MAX_FLOWS));
    });

    // No-op handler for alert events broadcast to the tenant group
    connection.on('AlertTriggered', () => {});

    connection.onreconnected(() => {
      setIsConnected(true);
      connection.invoke('JoinTenant', tenantId).catch(() => {});
    });

    connection.onclose(() => {
      setIsConnected(false);
    });

    connection
      .start()
      .then(() => {
        if (cancelled) return;
        setIsConnected(true);
        return connection.invoke('JoinTenant', tenantId);
      })
      .catch(() => {
        setIsConnected(false);
      });

    return () => {
      cancelled = true;
      connection.stop();
      connectionRef.current = null;
    };
  }, [tenantId]);

  return { recentFlows, isConnected, clearFlows, flowCount: recentFlows.length, sendPing, pongResult };
}
