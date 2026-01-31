'use client';

import { useProtocolDistribution } from '@/hooks/useDashboard';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { Spinner } from '@/components/ui/Spinner';
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

  // Filter out protocols with 0 bytes, sort by bytes descending
  const entries = data.entries
    .filter(e => e.totalBytes > 0)
    .sort((a, b) => b.totalBytes - a.totalBytes);

  const totalBytes = entries.reduce((sum, e) => sum + e.totalBytes, 0);

  return (
    <Card>
      <CardHeader><CardTitle>Protocol Distribution</CardTitle></CardHeader>
      <div className="flex items-center gap-6 px-4 pb-4">
        {/* Donut chart */}
        <div className="relative flex-shrink-0">
          <svg width="160" height="160" viewBox="0 0 160 160">
            {entries.length > 0 ? (
              <DonutSlices entries={entries} totalBytes={totalBytes} />
            ) : (
              <circle cx="80" cy="80" r="60" fill="none" stroke="#374151" strokeWidth="24" />
            )}
            <text x="80" y="76" textAnchor="middle" className="fill-gray-100 text-sm font-semibold">
              {formatBytes(totalBytes)}
            </text>
            <text x="80" y="94" textAnchor="middle" className="fill-gray-400 text-xs">
              total
            </text>
          </svg>
        </div>

        {/* Legend list */}
        <div className="flex-1 space-y-2">
          {entries.map((entry, idx) => {
            const pct = totalBytes > 0 ? (entry.totalBytes / totalBytes) * 100 : 0;
            return (
              <div key={entry.protocol} className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-2 min-w-0">
                  <span
                    className="h-3 w-3 rounded-sm flex-shrink-0"
                    style={{ backgroundColor: COLORS[idx % COLORS.length] }}
                  />
                  <span className="text-sm font-medium text-gray-200 truncate">{entry.protocol}</span>
                </div>
                <div className="flex items-center gap-3 flex-shrink-0">
                  <span className="text-xs text-gray-400">{entry.flowCount} flows</span>
                  <span className="text-sm font-medium text-gray-300 w-16 text-right">
                    {pct.toFixed(1)}%
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </Card>
  );
}

function DonutSlices({
  entries,
  totalBytes,
}: {
  entries: { protocol: string; totalBytes: number }[];
  totalBytes: number;
}) {
  const cx = 80;
  const cy = 80;
  const r = 60;
  const strokeWidth = 24;
  const circumference = 2 * Math.PI * r;

  let cumulativeOffset = 0;

  return (
    <>
      {entries.map((entry, idx) => {
        const fraction = totalBytes > 0 ? entry.totalBytes / totalBytes : 0;
        const dashLength = fraction * circumference;
        const dashGap = circumference - dashLength;
        const offset = -cumulativeOffset + circumference * 0.25; // start from top
        cumulativeOffset += dashLength;

        return (
          <circle
            key={entry.protocol}
            cx={cx}
            cy={cy}
            r={r}
            fill="none"
            stroke={COLORS[idx % COLORS.length]}
            strokeWidth={strokeWidth}
            strokeDasharray={`${dashLength} ${dashGap}`}
            strokeDashoffset={offset}
            className="transition-all duration-500"
          />
        );
      })}
    </>
  );
}
