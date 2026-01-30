'use client';

import Link from 'next/link';
import { useParams } from 'next/navigation';
import { TrafficDetail } from '@/components/features/traffic/TrafficDetail';
import { Button } from '@/components/ui/Button';

export default function TrafficDetailPage() {
  const params = useParams();
  const id = params.id as string;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/dashboard/traffic">
          <Button variant="ghost" size="sm">
            <svg className="mr-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
            </svg>
            Back
          </Button>
        </Link>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Flow Details</h1>
      </div>
      <TrafficDetail id={id} />
    </div>
  );
}
