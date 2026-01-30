import Link from 'next/link';
import { UserList } from '@/components/features/users/UserList';
import { Button } from '@/components/ui/Button';

export default function UsersPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Users</h1>
        <Link href="/dashboard/users/new">
          <Button>Add User</Button>
        </Link>
      </div>
      <UserList />
    </div>
  );
}
