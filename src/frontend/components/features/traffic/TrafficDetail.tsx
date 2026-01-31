'use client';

import Link from 'next/link';
import { useTrafficFlow } from '@/hooks/useTraffic';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Spinner } from '@/components/ui/Spinner';
import { formatBytes, formatDate, formatDuration, getProtocolColor, getStatusColor } from '@/lib/utils';

export function TrafficDetail({ id }: { id: string }) {
  const { data, isLoading } = useTrafficFlow(id);

  if (isLoading) return <Spinner className="py-12" />;
  if (!data) return <p className="text-sm text-gray-500">Flow not found</p>;

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader><CardTitle>Flow Details</CardTitle></CardHeader>
        <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Source</dt>
            <dd className="mt-1 font-mono text-sm">{data.sourceIp}:{data.sourcePort}</dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Destination</dt>
            <dd className="mt-1 font-mono text-sm">{data.destinationIp}:{data.destinationPort}</dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Protocol</dt>
            <dd className="mt-1"><Badge className={getProtocolColor(data.protocol)}>{data.protocol}</Badge></dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Duration</dt>
            <dd className="mt-1 text-sm">{formatDuration(data.flowDuration)}</dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Bytes Sent</dt>
            <dd className="mt-1 text-sm">{formatBytes(data.bytesSent)}</dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Bytes Received</dt>
            <dd className="mt-1 text-sm">{formatBytes(data.bytesReceived)}</dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Packets Sent</dt>
            <dd className="mt-1 text-sm">{data.packetsSent.toLocaleString()}</dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Packets Received</dt>
            <dd className="mt-1 text-sm">{data.packetsReceived.toLocaleString()}</dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Started At</dt>
            <dd className="mt-1 text-sm">{formatDate(data.startedAt)}</dd>
          </div>
          <div>
            <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Ended At</dt>
            <dd className="mt-1 text-sm">{formatDate(data.endedAt)}</dd>
          </div>
        </dl>
      </Card>

      {(data.processName || data.resolvedHostname || data.tlsSni) && (
        <Card>
          <CardHeader><CardTitle>Connection Info</CardTitle></CardHeader>
          <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {data.processName && (
              <div>
                <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Process</dt>
                <dd className="mt-1 text-sm font-medium">{data.processName}</dd>
              </div>
            )}
            {data.resolvedHostname && (
              <div>
                <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Hostname</dt>
                <dd className="mt-1 font-mono text-sm">{data.resolvedHostname}</dd>
              </div>
            )}
            {data.tlsSni && (
              <div className="sm:col-span-2">
                <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">TLS Server Name (SNI)</dt>
                <dd className="mt-1 font-mono text-sm">{data.tlsSni}</dd>
              </div>
            )}
          </dl>
        </Card>
      )}

      <div>
        <Link
          href={`/dashboard/traffic/${id}/packets`}
          className="inline-flex items-center gap-2 rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-500 transition-colors"
        >
          View Packets
        </Link>
      </div>

      {data.httpMetadata && (
        <Card>
          <CardHeader><CardTitle>HTTP Metadata</CardTitle></CardHeader>
          <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Method</dt>
              <dd className="mt-1 text-sm font-medium">{data.httpMetadata.method}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Status</dt>
              <dd className={`mt-1 text-sm font-medium ${getStatusColor(data.httpMetadata.statusCode)}`}>
                {data.httpMetadata.statusCode}
              </dd>
            </div>
            <div className="sm:col-span-2">
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">URL</dt>
              <dd className="mt-1 font-mono text-xs break-all">{data.httpMetadata.host}{data.httpMetadata.path}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Response Time</dt>
              <dd className="mt-1 text-sm">{data.httpMetadata.responseTimeMs.toFixed(2)}ms</dd>
            </div>
            {data.httpMetadata.contentType && (
              <div>
                <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">Content Type</dt>
                <dd className="mt-1 text-sm">{data.httpMetadata.contentType}</dd>
              </div>
            )}
            {data.httpMetadata.userAgent && (
              <div className="sm:col-span-2">
                <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">User Agent</dt>
                <dd className="mt-1 text-xs break-all">{data.httpMetadata.userAgent}</dd>
              </div>
            )}
          </dl>
        </Card>
      )}
    </div>
  );
}
