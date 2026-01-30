'use client';

import { useState } from 'react';
import { Select } from '@/components/ui/Select';
import { OverviewCards } from '@/components/features/dashboard/OverviewCards';
import { TopTalkersChart } from '@/components/features/dashboard/TopTalkersChart';
import { ProtocolChart } from '@/components/features/dashboard/ProtocolChart';
import { BandwidthChart } from '@/components/features/dashboard/BandwidthChart';
import { PERIOD_OPTIONS } from '@/lib/constants';

export default function DashboardPage() {
  const [period, setPeriod] = useState('24h');

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Dashboard</h1>
        <div className="w-48">
          <Select
            options={PERIOD_OPTIONS.map(p => ({ label: p.label, value: p.value }))}
            value={period}
            onChange={e => setPeriod(e.target.value)}
          />
        </div>
      </div>
      <OverviewCards period={period} />
      <BandwidthChart period={period} />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <TopTalkersChart period={period} />
        <ProtocolChart period={period} />
      </div>
    </div>
  );
}
