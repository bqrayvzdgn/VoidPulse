'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useUsers, useDeleteUser } from '@/hooks/useUsers';
import { useToast } from '@/providers/ToastProvider';
import { Table, Pagination } from '@/components/ui/Table';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { ConfirmModal } from '@/components/ui/Modal';
import { formatDate } from '@/lib/utils';
import type { UserResponse } from '@/types/user';

export function UserList() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useUsers(page);
  const deleteUser = useDeleteUser();
  const { addToast } = useToast();
  const [deleteTarget, setDeleteTarget] = useState<UserResponse | null>(null);

  const handleDelete = async () => {
    if (!deleteTarget) return;
    const result = await deleteUser.mutateAsync(deleteTarget.id);
    if (result.success) {
      addToast('success', 'User deleted');
    } else {
      addToast('error', result.error?.message || 'Failed to delete user');
    }
    setDeleteTarget(null);
  };

  const columns = [
    { key: 'name', header: 'Name', render: (u: UserResponse) => <span className="font-medium">{u.fullName}</span> },
    { key: 'email', header: 'Email', render: (u: UserResponse) => u.email },
    {
      key: 'roles',
      header: 'Roles',
      render: (u: UserResponse) => (
        <div className="flex gap-1 flex-wrap">
          {u.roles.map(r => <Badge key={r} variant="info">{r}</Badge>)}
        </div>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      render: (u: UserResponse) => <Badge variant={u.isActive ? 'success' : 'danger'}>{u.isActive ? 'Active' : 'Inactive'}</Badge>,
    },
    {
      key: 'lastLogin',
      header: 'Last Login',
      render: (u: UserResponse) => <span className="text-xs">{u.lastLoginAt ? formatDate(u.lastLoginAt) : 'Never'}</span>,
    },
    {
      key: 'actions',
      header: '',
      render: (u: UserResponse) => (
        <div className="flex gap-2">
          <Link href={`/dashboard/users/${u.id}/edit`} className="text-brand-600 hover:text-brand-700 text-xs font-medium">Edit</Link>
          <button onClick={() => setDeleteTarget(u)} className="text-red-600 hover:text-red-700 text-xs font-medium">Delete</button>
        </div>
      ),
    },
  ];

  if (isLoading) return <Spinner className="py-12" />;

  return (
    <>
      <Table columns={columns} data={data?.items ?? []} keyExtractor={(u) => u.id} emptyMessage="No users found" />
      {data && <Pagination page={data.page} totalPages={data.totalPages} onPageChange={setPage} />}
      <ConfirmModal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Delete User"
        message={`Are you sure you want to delete "${deleteTarget?.fullName}"? This action cannot be undone.`}
        loading={deleteUser.isPending}
      />
    </>
  );
}
