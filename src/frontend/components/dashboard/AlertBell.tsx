'use client';

import { useState, useRef, useEffect } from 'react';
import Link from 'next/link';
import { useUnacknowledgedCount, useAlertHub } from '@/hooks/useAlerts';
import { ALERT_SEVERITY_LABELS } from '@/types/alerts';
import { cn } from '@/lib/utils';

const severityColors: Record<number, string> = {
  0: 'text-blue-600 dark:text-blue-400',
  1: 'text-yellow-600 dark:text-yellow-400',
  2: 'text-red-600 dark:text-red-400',
};

export function AlertBell() {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const { data: countData } = useUnacknowledgedCount();
  const { recentAlerts } = useAlertHub();

  const count = countData?.unacknowledgedCount ?? 0;

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, []);

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen(!open)}
        className="relative rounded-md p-2 text-gray-600 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-800"
        aria-label="Alerts"
      >
        <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
          <path strokeLinecap="round" strokeLinejoin="round" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
        </svg>
        {count > 0 && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 min-w-[1rem] items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-bold text-white">
            {count > 99 ? '99+' : count}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-full z-50 mt-2 w-80 rounded-lg border border-gray-200 bg-white shadow-lg dark:border-gray-700 dark:bg-gray-800">
          <div className="flex items-center justify-between border-b border-gray-200 px-4 py-3 dark:border-gray-700">
            <span className="text-sm font-semibold text-gray-900 dark:text-gray-100">Alerts</span>
            {count > 0 && (
              <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-700 dark:bg-red-900/50 dark:text-red-300">
                {count} unread
              </span>
            )}
          </div>
          <div className="max-h-72 overflow-y-auto">
            {recentAlerts.length > 0 ? (
              recentAlerts.slice(0, 10).map((alert) => (
                <div
                  key={alert.id}
                  className={cn(
                    'border-b border-gray-100 px-4 py-3 last:border-0 dark:border-gray-700',
                    !alert.isAcknowledged && 'bg-gray-50 dark:bg-gray-750'
                  )}
                >
                  <div className="flex items-start justify-between gap-2">
                    <span className={cn('text-xs font-medium', severityColors[alert.severity])}>
                      {ALERT_SEVERITY_LABELS[alert.severity]}
                    </span>
                    <span className="text-xs text-gray-400">
                      {new Date(alert.triggeredAt).toLocaleTimeString()}
                    </span>
                  </div>
                  <p className="mt-1 text-xs text-gray-700 dark:text-gray-300 line-clamp-2">
                    {alert.message}
                  </p>
                </div>
              ))
            ) : (
              <div className="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
                No recent alerts
              </div>
            )}
          </div>
          <div className="border-t border-gray-200 px-4 py-2 dark:border-gray-700">
            <Link
              href="/dashboard/alerts"
              className="block text-center text-sm font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400"
              onClick={() => setOpen(false)}
            >
              View all alerts
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
