'use client';

import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { useUser, useUpdateUser } from '@/hooks/useUsers';
import { useToast } from '@/providers/ToastProvider';
import { UserForm } from '@/components/features/users/UserForm';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Spinner } from '@/components/ui/Spinner';

export default function EditUserPage() {
  const router = useRouter();
  const params = useParams();
  const id = params.id as string;
  const { data: user, isLoading } = useUser(id);
  const updateUser = useUpdateUser(id);
  const { addToast } = useToast();

  const handleSubmit = async (data: any) => {
    const payload: any = { fullName: data.fullName, roles: data.roles };
    const result = await updateUser.mutateAsync(payload);
    if (result.success) {
      addToast('success', 'User updated successfully');
      router.push('/dashboard/users');
    } else {
      addToast('error', result.error?.message || 'Failed to update user');
    }
  };

  if (isLoading) return <Spinner className="py-12" />;
  if (!user) return <p className="text-sm text-gray-500">User not found</p>;

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
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Edit User</h1>
      </div>
      <Card className="max-w-lg">
        <UserForm
          defaultValues={{ email: user.email, fullName: user.fullName, roles: user.roles }}
          onSubmit={handleSubmit}
          isSubmitting={updateUser.isPending}
          isEdit
        />
      </Card>
    </div>
  );
}
