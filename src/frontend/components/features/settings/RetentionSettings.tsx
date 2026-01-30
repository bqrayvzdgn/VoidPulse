'use client';

import { useEffect, useState } from 'react';
import { useRetentionPolicy, useUpdateRetentionPolicy } from '@/hooks/useRetention';
import { useToast } from '@/providers/ToastProvider';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';

export function RetentionSettings() {
  const { data, isLoading } = useRetentionPolicy();
  const updatePolicy = useUpdateRetentionPolicy();
  const { addToast } = useToast();
  const [days, setDays] = useState('90');

  useEffect(() => {
    if (data) setDays(String(data.retentionDays));
  }, [data]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const retentionDays = parseInt(days, 10);
    if (isNaN(retentionDays) || retentionDays < 1) {
      addToast('error', 'Retention days must be at least 1');
      return;
    }
    const result = await updatePolicy.mutateAsync({ retentionDays });
    if (result.success) {
      addToast('success', 'Retention policy updated');
    } else {
      addToast('error', result.error?.message || 'Failed to update retention policy');
    }
  };

  if (isLoading) return <Spinner className="py-8" />;

  return (
    <Card>
      <CardHeader><CardTitle>Data Retention</CardTitle></CardHeader>
      <form onSubmit={handleSubmit} className="flex items-end gap-4">
        <div className="flex-1 max-w-xs">
          <Input
            label="Retention Period (days)"
            type="number"
            min="1"
            value={days}
            onChange={e => setDays(e.target.value)}
          />
        </div>
        <Button type="submit" loading={updatePolicy.isPending}>Save</Button>
      </form>
      <p className="mt-3 text-xs text-gray-500 dark:text-gray-400">
        Traffic data older than this will be automatically purged.
      </p>
    </Card>
  );
}
