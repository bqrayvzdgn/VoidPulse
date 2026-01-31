'use client';

import { useState, useEffect, useCallback } from 'react';
import { useTrafficHub, type LiveTrafficFlow } from '@/hooks/useTrafficHub';
import { useTrafficFlows } from '@/hooks/useTraffic';
import { formatBytes, formatDateShort } from '@/lib/utils';
import { cn } from '@/lib/utils';

const REFRESH_OPTIONS = [
  { label: 'Off', value: 0 },
  { label: '3s', value: 3 },
  { label: '5s', value: 5 },
  { label: '10s', value: 10 },
  { label: '30s', value: 30 },
];

export default function LivePage() {
  const { recentFlows, isConnected, clearFlows } = useTrafficHub();
  const [paused, setPaused] = useState(false);
  const [pausedFlows, setPausedFlows] = useState<LiveTrafficFlow[]>([]);
  const [refreshInterval, setRefreshInterval] = useState(5);
  const [initialLoaded, setInitialLoaded] = useState(false);
  const [mergedFlows, setMergedFlows] = useState<LiveTrafficFlow[]>([]);

  // Fetch initial/polling data from REST API
  const { data: apiData, refetch } = useTrafficFlows({
    page: 1,
    pageSize: 100,
    sortBy: 'createdAt',
    sortOrder: 'desc',
  });

  // Helper to convert API flow to LiveTrafficFlow
  const toLiveFlow = useCallback((f: NonNullable<typeof apiData>['items'][number]): LiveTrafficFlow => ({
    id: f.id,
    sourceIp: f.sourceIp,
    destinationIp: f.destinationIp,
    sourcePort: f.sourcePort,
    destinationPort: f.destinationPort,
    protocol: f.protocol,
    bytesSent: f.bytesSent,
    bytesReceived: f.bytesReceived,
    packetsSent: f.packetsSent,
    packetsReceived: f.packetsReceived,
    startedAt: f.startedAt,
    endedAt: f.endedAt,
    flowDuration: f.flowDuration,
    processName: f.processName ?? undefined,
    resolvedHostname: f.resolvedHostname ?? undefined,
    tlsSni: f.tlsSni ?? undefined,
    createdAt: f.createdAt,
  }), []);

  // Load initial data once
  useEffect(() => {
    if (apiData?.items && !initialLoaded) {
      setMergedFlows(apiData.items.map(toLiveFlow));
      setInitialLoaded(true);
    }
  }, [apiData, initialLoaded, toLiveFlow]);

  // Merge SignalR real-time flows on top
  useEffect(() => {
    if (recentFlows.length > 0 && initialLoaded) {
      setMergedFlows((prev) => {
        const realtimeIds = new Set(recentFlows.map((f) => f.id));
        const filtered = prev.filter((f) => !realtimeIds.has(f.id));
        return [...recentFlows, ...filtered].slice(0, 200);
      });
    }
  }, [recentFlows, initialLoaded]);

  // Polling refetch
  useEffect(() => {
    if (refreshInterval <= 0 || paused) return;
    const timer = setInterval(() => refetch(), refreshInterval * 1000);
    return () => clearInterval(timer);
  }, [refreshInterval, paused, refetch]);

  // Merge polled data
  useEffect(() => {
    if (apiData?.items && initialLoaded) {
      const apiFlows = apiData.items.map(toLiveFlow);
      setMergedFlows((prev) => {
        const existingIds = new Set(prev.map((f) => f.id));
        const newFlows = apiFlows.filter((f) => !existingIds.has(f.id));
        if (newFlows.length === 0) return prev;
        return [...newFlows, ...prev].slice(0, 200);
      });
    }
  }, [apiData, initialLoaded, toLiveFlow]);

  const flows = paused ? pausedFlows : mergedFlows;

  const handlePause = () => {
    if (!paused) setPausedFlows([...mergedFlows]);
    setPaused(!paused);
  };

  const handleClear = useCallback(() => {
    clearFlows();
    setMergedFlows([]);
    setInitialLoaded(false);
  }, [clearFlows]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Live Traffic</h1>
          <span
            className={cn(
              'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium',
              isConnected
                ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400'
                : 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400'
            )}
          >
            <span
              className={cn(
                'h-2 w-2 rounded-full',
                isConnected ? 'bg-green-500 animate-pulse' : 'bg-red-500'
              )}
            />
            {isConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 rounded-md border border-gray-200 bg-white px-2 py-1 dark:border-gray-600 dark:bg-gray-800">
            <span className="text-xs text-gray-500 dark:text-gray-400">Refresh:</span>
            {REFRESH_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                onClick={() => setRefreshInterval(opt.value)}
                className={cn(
                  'rounded px-2 py-0.5 text-xs font-medium transition-colors',
                  refreshInterval === opt.value
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-600 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-700'
                )}
              >
                {opt.label}
              </button>
            ))}
          </div>
          <span className="text-sm text-gray-500 dark:text-gray-400">
            {flows.length} flows
          </span>
          <button
            onClick={handlePause}
            className={cn(
              'rounded-md px-3 py-1.5 text-sm font-medium transition-colors',
              paused
                ? 'bg-green-600 text-white hover:bg-green-700'
                : 'bg-yellow-600 text-white hover:bg-yellow-700'
            )}
          >
            {paused ? 'Resume' : 'Pause'}
          </button>
          <button
            onClick={handleClear}
            className="rounded-md bg-gray-200 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-300 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600"
          >
            Clear
          </button>
        </div>
      </div>

      <div className="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
        <div className="max-h-[calc(100vh-12rem)] overflow-y-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="sticky top-0 bg-gray-50 dark:bg-gray-800">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Time</th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Protocol</th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Source</th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Destination</th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Hostname</th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Process</th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Bytes</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 bg-white dark:divide-gray-700 dark:bg-gray-900">
              {flows.length > 0 ? (
                flows.map((flow) => (
                  <tr key={flow.id} className="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                    <td className="whitespace-nowrap px-4 py-2 text-xs text-gray-500 dark:text-gray-400">
                      {formatDateShort(flow.createdAt)}
                    </td>
                    <td className="whitespace-nowrap px-4 py-2">
                      <span className={cn(
                        'inline-flex rounded px-1.5 py-0.5 text-xs font-medium',
                        flow.protocol === 'TCP'
                          ? 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400'
                          : flow.protocol === 'UDP'
                            ? 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-400'
                            : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
                      )}>
                        {flow.protocol}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-4 py-2 text-xs font-mono text-gray-700 dark:text-gray-300">
                      {flow.sourceIp}:{flow.sourcePort}
                    </td>
                    <td className="whitespace-nowrap px-4 py-2 text-xs font-mono text-gray-700 dark:text-gray-300">
                      {flow.destinationIp}:{flow.destinationPort}
                    </td>
                    <td className="max-w-[200px] truncate px-4 py-2 text-xs text-gray-700 dark:text-gray-300" title={flow.resolvedHostname || flow.tlsSni || ''}>
                      {flow.resolvedHostname || flow.tlsSni || '-'}
                    </td>
                    <td className="whitespace-nowrap px-4 py-2 text-xs text-gray-600 dark:text-gray-400">
                      {flow.processName || '-'}
                    </td>
                    <td className="whitespace-nowrap px-4 py-2 text-xs text-gray-600 dark:text-gray-400">
                      {formatBytes(flow.bytesSent + flow.bytesReceived)}
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={7} className="px-4 py-12 text-center text-sm text-gray-500 dark:text-gray-400">
                    {isConnected
                      ? 'Loading traffic data...'
                      : 'Connecting to live feed...'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
