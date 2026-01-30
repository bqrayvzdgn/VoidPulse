'use client';

import { useForm } from 'react-hook-form';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import type { TrafficQueryParams } from '@/types/traffic';

interface TrafficFiltersProps {
  filters: TrafficQueryParams;
  onApply: (filters: TrafficQueryParams) => void;
  onExport: () => void;
}

export function TrafficFilters({ filters, onApply, onExport }: TrafficFiltersProps) {
  const { register, handleSubmit, reset } = useForm<TrafficQueryParams>({
    defaultValues: filters,
  });

  const onSubmit = (data: TrafficQueryParams) => {
    onApply({ ...data, page: 1 });
  };

  const handleReset = () => {
    reset({ sourceIp: '', destinationIp: '', protocol: '', startDate: '', endDate: '' });
    onApply({ page: 1, pageSize: 20 });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 rounded-lg border border-gray-200 bg-white p-4 dark:border-gray-700 dark:bg-gray-800">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <Input label="Source IP" placeholder="e.g. 192.168.1.1" {...register('sourceIp')} />
        <Input label="Destination IP" placeholder="e.g. 10.0.0.1" {...register('destinationIp')} />
        <Select
          label="Protocol"
          options={[
            { label: 'All', value: '' },
            { label: 'TCP', value: 'TCP' },
            { label: 'UDP', value: 'UDP' },
            { label: 'ICMP', value: 'ICMP' },
          ]}
          {...register('protocol')}
        />
        <Input label="Start Date" type="datetime-local" {...register('startDate')} />
        <Input label="End Date" type="datetime-local" {...register('endDate')} />
      </div>
      <div className="flex items-center gap-3">
        <Button type="submit" size="sm">Apply Filters</Button>
        <Button type="button" variant="secondary" size="sm" onClick={handleReset}>Reset</Button>
        <Button type="button" variant="ghost" size="sm" onClick={onExport}>Export CSV</Button>
      </div>
    </form>
  );
}
