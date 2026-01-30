'use client';

import { useState } from 'react';
import { useSavedFilters, useDeleteSavedFilter } from '@/hooks/useSavedFilters';
import { useToast } from '@/providers/ToastProvider';
import { Card, CardHeader, CardTitle } from '@/components/ui/Card';
import { Spinner } from '@/components/ui/Spinner';
import { Button } from '@/components/ui/Button';
import { ConfirmModal } from '@/components/ui/Modal';
import { formatDate } from '@/lib/utils';
import type { SavedFilterResponse } from '@/types/savedFilter';

export function SavedFiltersList() {
  const { data, isLoading } = useSavedFilters();
  const deleteFilter = useDeleteSavedFilter();
  const { addToast } = useToast();
  const [deleteTarget, setDeleteTarget] = useState<SavedFilterResponse | null>(null);

  const handleDelete = async () => {
    if (!deleteTarget) return;
    const result = await deleteFilter.mutateAsync(deleteTarget.id);
    if (result.success) {
      addToast('success', 'Saved filter deleted');
    } else {
      addToast('error', result.error?.message || 'Failed to delete filter');
    }
    setDeleteTarget(null);
  };

  if (isLoading) return <Spinner className="py-8" />;

  return (
    <Card>
      <CardHeader><CardTitle>Saved Filters</CardTitle></CardHeader>
      {!data?.length ? (
        <p className="text-sm text-gray-500 dark:text-gray-400">No saved filters yet. Save filters from the Traffic page.</p>
      ) : (
        <div className="space-y-3">
          {data.map(filter => (
            <div key={filter.id} className="flex items-center justify-between rounded-md border border-gray-200 p-3 dark:border-gray-700">
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-gray-100">{filter.name}</p>
                <p className="text-xs text-gray-500 dark:text-gray-400">Created {formatDate(filter.createdAt)}</p>
              </div>
              <Button variant="ghost" size="sm" onClick={() => setDeleteTarget(filter)}>
                <svg className="h-4 w-4 text-red-500" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clipRule="evenodd" />
                </svg>
              </Button>
            </div>
          ))}
        </div>
      )}
      <ConfirmModal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Delete Saved Filter"
        message={`Are you sure you want to delete "${deleteTarget?.name}"?`}
        loading={deleteFilter.isPending}
      />
    </Card>
  );
}
