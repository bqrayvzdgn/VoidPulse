'use client';

import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { useCreateUser } from '@/hooks/useUsers';
import { useToast } from '@/providers/ToastProvider';
import { UserForm } from '@/components/features/users/UserForm';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';

export default function NewUserPage() {
  const router = useRouter();
  const createUser = useCreateUser();
  const { addToast } = useToast();

  const handleSubmit = async (data: any) => {
    const result = await createUser.mutateAsync(data);
    if (result.success) {
      addToast('success', 'User created successfully');
      router.push('/dashboard/users');
    } else {
      addToast('error', result.error?.message || 'Failed to create user');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link href="/dashboard/users">
          <Button variant="ghost" size="sm">
            <svg className="mr-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
            </svg>
            Back
          </Button>
        </Link>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Add User</h1>
      </div>
      <Card className="max-w-lg">
        <UserForm onSubmit={handleSubmit} isSubmitting={createUser.isPending} />
      </Card>
    </div>
  );
}
