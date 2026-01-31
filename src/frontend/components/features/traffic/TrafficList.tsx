'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useTrafficFlows, exportTrafficCsv } from '@/hooks/useTraffic';
import { useSavedFilters, useCreateSavedFilter } from '@/hooks/useSavedFilters';
import { useToast } from '@/providers/ToastProvider';
import { Table, Pagination } from '@/components/ui/Table';
import { Badge } from '@/components/ui/Badge';
import { Spinner } from '@/components/ui/Spinner';
import { TrafficFilters } from './TrafficFilters';
import { formatBytes, formatDate, formatDuration, getProtocolColor } from '@/lib/utils';
import type { TrafficQueryParams } from '@/types/traffic';

export function TrafficList() {
  const [filters, setFilters] = useState<TrafficQueryParams>({ page: 1, pageSize: 20 });
  const { data, isLoading } = useTrafficFlows(filters);
  const { addToast } = useToast();

  const columns = [
    {
      key: 'source',
      header: 'Source',
      render: (item: any) => (
        <div>
          <span className="font-mono text-xs">{item.sourceIp}</span>
          <span className="text-gray-400">:{item.sourcePort}</span>
        </div>
      ),
    },
    {
      key: 'destination',
      header: 'Destination',
      render: (item: any) => (
        <div>
          <span className="font-mono text-xs">{item.destinationIp}</span>
          <span className="text-gray-400">:{item.destinationPort}</span>
          {(item.resolvedHostname || item.tlsSni) && (
            <div className="text-xs text-gray-500 dark:text-gray-400 truncate max-w-[200px]" title={item.resolvedHostname || item.tlsSni}>
              {item.tlsSni || item.resolvedHostname}
            </div>
          )}
        </div>
      ),
    },
    {
      key: 'process',
      header: 'Process',
      render: (item: any) => (
        <span className="text-xs">{item.processName || '—'}</span>
      ),
    },
    {
      key: 'protocol',
      header: 'Protocol',
      render: (item: any) => <Badge className={getProtocolColor(item.protocol)}>{item.protocol}</Badge>,
    },
    {
      key: 'bytes',
      header: 'Data',
      render: (item: any) => (
        <div className="text-xs">
          <div>Sent: {formatBytes(item.bytesSent)}</div>
          <div>Recv: {formatBytes(item.bytesReceived)}</div>
        </div>
      ),
    },
    {
      key: 'duration',
      header: 'Duration',
      render: (item: any) => <span className="text-xs">{formatDuration(item.flowDuration)}</span>,
    },
    {
      key: 'time',
      header: 'Time',
      render: (item: any) => <span className="text-xs">{formatDate(item.startedAt)}</span>,
    },
    {
      key: 'actions',
      header: '',
      render: (item: any) => (
        <Link href={`/dashboard/traffic/${item.id}`} className="text-brand-600 hover:text-brand-700 text-xs font-medium">
          View
        </Link>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <TrafficFilters
        filters={filters}
        onApply={setFilters}
        onExport={() => {
          exportTrafficCsv(filters);
          addToast('info', 'Exporting traffic data...');
        }}
      />
      {isLoading ? (
        <Spinner className="py-12" />
      ) : (
        <>
          <Table
            columns={columns}
            data={data?.items ?? []}
            keyExtractor={(item) => item.id}
            emptyMessage="No traffic flows found"
          />
          {data && (
            <Pagination
              page={data.page}
              totalPages={data.totalPages}
              onPageChange={(page) => setFilters(prev => ({ ...prev, page }))}
            />
          )}
        </>
      )}
    </div>
  );
}
