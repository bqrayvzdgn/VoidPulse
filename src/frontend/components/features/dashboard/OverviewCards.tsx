'use client';

import { useOverview } from '@/hooks/useDashboard';
import { Card } from '@/components/ui/Card';
import { Spinner } from '@/components/ui/Spinner';
import { formatBytes, formatNumber } from '@/lib/utils';

interface StatCardProps {
  label: string;
  value: string;
}

function StatCard({ label, value }: StatCardProps) {
  return (
    <Card>
      <p className="text-sm font-medium text-gray-500 dark:text-gray-400">{label}</p>
      <p className="mt-1 text-2xl font-bold text-gray-900 dark:text-gray-100">{value}</p>
    </Card>
  );
}

export function OverviewCards({ period }: { period: string }) {
  const { data, isLoading } = useOverview(period);

  if (isLoading) return <Spinner className="py-8" />;
  if (!data) return null;

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
      <StatCard label="Total Flows" value={formatNumber(data.totalFlows)} />
      <StatCard label="Total Bytes" value={formatBytes(data.totalBytes)} />
      <StatCard label="Active Agents" value={formatNumber(data.activeAgents)} />
      <StatCard label="Unique Sources" value={formatNumber(data.uniqueSourceIps)} />
      <StatCard label="Unique Destinations" value={formatNumber(data.uniqueDestinationIps)} />
    </div>
  );
}
