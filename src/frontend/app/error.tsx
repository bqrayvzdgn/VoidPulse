'use client';

import { Button } from '@/components/ui/Button';

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4">
      <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Something went wrong</h2>
      <p className="text-sm text-gray-600 dark:text-gray-400">{error.message || 'An unexpected error occurred'}</p>
      <Button onClick={reset}>Try Again</Button>
    </div>
  );
}
