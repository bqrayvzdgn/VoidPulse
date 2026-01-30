'use client';

import { useProtocolDistribution } from '@/hooks/useDashboard';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { Spinner } from '@/components/ui/Spinner';
import { PieChart, Pie, Cell, Tooltip, ResponsiveContainer, Legend } from 'recharts';
import { formatBytes } from '@/lib/utils';

const COLORS = ['#6366f1', '#22c55e', '#eab308', '#ef4444', '#06b6d4', '#f97316'];

export function ProtocolChart({ period }: { period: string }) {
  const { data, isLoading } = useProtocolDistribution(period);

  if (isLoading) return <Card><Spinner className="py-12" /></Card>;
  if (!data?.entries?.length) {
    return (
      <Card>
        <CardHeader><CardTitle>Protocol Distribution</CardTitle></CardHeader>
        <p className="text-sm text-gray-500 dark:text-gray-400 text-center py-8">No data available</p>
      </Card>
    );
  }

  const chartData = data.entries.map(e => ({
    name: e.protocol,
    value: e.totalBytes,
    percentage: e.percentage,
    flows: e.flowCount,
  }));

  return (
    <Card>
      <CardHeader><CardTitle>Protocol Distribution</CardTitle></CardHeader>
      <div className="h-64">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={chartData}
              cx="50%"
              cy="50%"
              outerRadius={80}
              dataKey="value"
              label={({ name, percentage }) => `${name} (${percentage.toFixed(1)}%)`}
              fontSize={12}
            >
              {chartData.map((_, index) => (
                <Cell key={index} fill={COLORS[index % COLORS.length]} />
              ))}
            </Pie>
            <Tooltip formatter={(value: number) => formatBytes(value)} />
            <Legend />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </Card>
  );
}
