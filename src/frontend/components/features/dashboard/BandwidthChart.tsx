'use client';

import { useBandwidth } from '@/hooks/useDashboard';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { Spinner } from '@/components/ui/Spinner';
import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts';
import { formatBytes, formatDateShort } from '@/lib/utils';

export function BandwidthChart({ period }: { period: string }) {
  const { data, isLoading } = useBandwidth(period);

  if (isLoading) return <Card><Spinner className="py-12" /></Card>;
  if (!data?.entries?.length) {
    return (
      <Card>
        <CardHeader><CardTitle>Bandwidth Over Time</CardTitle></CardHeader>
        <p className="text-sm text-gray-500 dark:text-gray-400 text-center py-8">No data available</p>
      </Card>
    );
  }

  const chartData = data.entries.map(e => ({
    time: formatDateShort(e.timestamp),
    sent: e.bytesSent,
    received: e.bytesReceived,
    total: e.totalBytes,
  }));

  return (
    <Card>
      <CardHeader><CardTitle>Bandwidth Over Time</CardTitle></CardHeader>
      <div className="h-64">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" className="opacity-30" />
            <XAxis dataKey="time" fontSize={12} />
            <YAxis tickFormatter={(v) => formatBytes(v)} fontSize={12} />
            <Tooltip formatter={(value: number) => formatBytes(value)} />
            <Area type="monotone" dataKey="sent" stackId="1" stroke="#6366f1" fill="#6366f1" fillOpacity={0.4} name="Sent" />
            <Area type="monotone" dataKey="received" stackId="1" stroke="#22c55e" fill="#22c55e" fillOpacity={0.4} name="Received" />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </Card>
  );
}
