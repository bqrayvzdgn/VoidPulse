'use client';

import { useTopTalkers } from '@/hooks/useDashboard';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { Spinner } from '@/components/ui/Spinner';
import { formatBytes } from '@/lib/utils';
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';

export function TopTalkersChart({ period }: { period: string }) {
  const { data, isLoading } = useTopTalkers(period);

  if (isLoading) return <Card><Spinner className="py-12" /></Card>;
  if (!data?.entries?.length) {
    return (
      <Card>
        <CardHeader><CardTitle>Top Talkers</CardTitle></CardHeader>
        <p className="text-sm text-gray-500 dark:text-gray-400 text-center py-8">No data available</p>
      </Card>
    );
  }

  const chartData = data.entries.map(e => ({
    ip: e.ip.length > 15 ? e.ip.slice(0, 12) + '...' : e.ip,
    fullIp: e.ip,
    bytes: e.totalBytes,
    flows: e.flowCount,
  }));

  return (
    <Card>
      <CardHeader><CardTitle>Top Talkers</CardTitle></CardHeader>
      <div className="h-64">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={chartData} layout="vertical" margin={{ left: 20 }}>
            <XAxis type="number" tickFormatter={(v) => formatBytes(v)} fontSize={12} />
            <YAxis type="category" dataKey="ip" width={110} fontSize={12} />
            <Tooltip
              formatter={(value: number) => formatBytes(value)}
              labelFormatter={(label, payload) => payload?.[0]?.payload?.fullIp || label}
            />
            <Bar dataKey="bytes" fill="#6366f1" radius={[0, 4, 4, 0]} name="Traffic" />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </Card>
  );
}
