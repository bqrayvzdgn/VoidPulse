'use client';

import { useAuth } from '@/providers/AuthProvider';
import { Button } from '@/components/ui/Button';

export function Header() {
  const { user, logout } = useAuth();

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b border-gray-200 bg-white px-6 dark:border-gray-700 dark:bg-gray-900">
      <div className="flex items-center gap-4">
        <h1 className="text-lg font-bold text-brand-600 dark:text-brand-400">VoidPulse</h1>
      </div>
      <div className="flex items-center gap-4">
        {user && (
          <>
            <div className="text-sm text-gray-600 dark:text-gray-400">
              <span className="font-medium text-gray-900 dark:text-gray-100">{user.fullName}</span>
              <span className="ml-2 text-xs text-gray-500">({user.roles.join(', ')})</span>
            </div>
            <Button variant="ghost" size="sm" onClick={logout}>
              Logout
            </Button>
          </>
        )}
      </div>
    </header>
  );
}
