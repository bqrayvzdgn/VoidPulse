'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useAlerts, useAcknowledgeAlert } from '@/hooks/useAlerts';
import { Spinner } from '@/components/ui/Spinner';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import { formatDate } from '@/lib/utils';
import { ALERT_SEVERITY_LABELS, ALERT_CONDITION_LABELS } from '@/types/alerts';
import { cn } from '@/lib/utils';

const severityBadge: Record<number, string> = {
  0: 'bg-blue-100 text-blue-800 dark:bg-blue-900/50 dark:text-blue-300',
  1: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300',
  2: 'bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-300',
};

export default function AlertsPage() {
  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<string>('all');
  const pageSize = 20;

  const isAcknowledged = filter === 'acknowledged' ? true : filter === 'unacknowledged' ? false : null;
  const { data, isLoading } = useAlerts(isAcknowledged, null, page, pageSize);
  const acknowledgeMutation = useAcknowledgeAlert();

  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Alerts</h1>
        <div className="flex items-center gap-3">
          <Select
            options={[
              { label: 'All', value: 'all' },
              { label: 'Unacknowledged', value: 'unacknowledged' },
              { label: 'Acknowledged', value: 'acknowledged' },
            ]}
            value={filter}
            onChange={e => { setFilter(e.target.value); setPage(1); }}
          />
          <Link href="/dashboard/alerts/rules">
            <Button variant="primary" size="sm">Manage Rules</Button>
          </Link>
        </div>
      </div>

      {isLoading ? (
        <Spinner className="py-12" />
      ) : (
        <>
          <div className="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead className="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Severity</th>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Rule</th>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Message</th>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Source</th>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Triggered</th>
                  <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Status</th>
                  <th className="px-6 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 bg-white dark:divide-gray-700 dark:bg-gray-900">
                {data?.items && data.items.length > 0 ? (
                  data.items.map(alert => (
                    <tr key={alert.id} className={cn('hover:bg-gray-50 dark:hover:bg-gray-800', !alert.isAcknowledged && 'bg-gray-50/50 dark:bg-gray-800/30')}>
                      <td className="whitespace-nowrap px-6 py-4">
                        <span className={cn('inline-flex rounded-full px-2 py-0.5 text-xs font-medium', severityBadge[alert.severity])}>
                          {ALERT_SEVERITY_LABELS[alert.severity]}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-sm font-medium text-gray-900 dark:text-gray-100">
                        {alert.ruleName}
                      </td>
                      <td className="max-w-xs truncate px-6 py-4 text-sm text-gray-600 dark:text-gray-400">
                        {alert.message}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-sm font-mono text-gray-600 dark:text-gray-400">
                        {alert.sourceIp || '-'}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-600 dark:text-gray-400">
                        {formatDate(alert.triggeredAt)}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-sm">
                        {alert.isAcknowledged ? (
                          <span className="text-green-600 dark:text-green-400">Acknowledged</span>
                        ) : (
                          <span className="text-orange-600 dark:text-orange-400">Pending</span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-right">
                        {!alert.isAcknowledged && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => acknowledgeMutation.mutate(alert.id)}
                            disabled={acknowledgeMutation.isPending}
                          >
                            Acknowledge
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={7} className="px-6 py-12 text-center">
                      <div className="text-sm text-gray-500 dark:text-gray-400">
                        No alerts found.
                      </div>
                      <p className="mt-2 text-xs text-gray-400 dark:text-gray-500">
                        Alerts are triggered by rules. Go to{' '}
                        <Link href="/dashboard/alerts/rules" className="text-blue-600 hover:underline dark:text-blue-400">
                          Manage Rules
                        </Link>{' '}
                        to create alert rules (e.g., byte threshold, port scan detection, protocol anomaly).
                      </p>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-600 dark:text-gray-400">
                Page {page} of {totalPages} ({data?.totalCount} total)
              </span>
              <div className="flex gap-2">
                <Button variant="secondary" size="sm" onClick={() => setPage(p => p - 1)} disabled={page <= 1}>
                  Previous
                </Button>
                <Button variant="secondary" size="sm" onClick={() => setPage(p => p + 1)} disabled={page >= totalPages}>
                  Next
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
