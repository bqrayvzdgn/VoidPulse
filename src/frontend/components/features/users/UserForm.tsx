'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { ROLES } from '@/lib/constants';

const schema = z.object({
  email: z.string().email('Invalid email'),
  fullName: z.string().min(1, 'Full name is required'),
  password: z.string().min(8, 'Password must be at least 8 characters').optional().or(z.literal('')),
  roles: z.array(z.string()).min(1, 'At least one role is required'),
});

type FormData = z.infer<typeof schema>;

interface UserFormProps {
  defaultValues?: Partial<FormData>;
  onSubmit: (data: FormData) => void;
  isSubmitting: boolean;
  isEdit?: boolean;
}

const roleOptions = [ROLES.TENANT_ADMIN, ROLES.ANALYST, ROLES.VIEWER];

export function UserForm({ defaultValues, onSubmit, isSubmitting, isEdit }: UserFormProps) {
  const { register, handleSubmit, formState: { errors }, setValue, watch } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { roles: [], ...defaultValues },
  });

  const selectedRoles = watch('roles') || [];

  const toggleRole = (role: string) => {
    const current = selectedRoles;
    if (current.includes(role)) {
      setValue('roles', current.filter(r => r !== role));
    } else {
      setValue('roles', [...current, role]);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <Input label="Full Name" error={errors.fullName?.message} {...register('fullName')} />
      <Input label="Email" type="email" error={errors.email?.message} {...register('email')} />
      <Input
        label={isEdit ? 'New Password (leave blank to keep current)' : 'Password'}
        type="password"
        error={errors.password?.message}
        {...register('password')}
      />
      <div className="space-y-2">
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">Roles</label>
        <div className="flex gap-2 flex-wrap">
          {roleOptions.map(role => (
            <button
              key={role}
              type="button"
              onClick={() => toggleRole(role)}
              className={`rounded-full px-3 py-1 text-xs font-medium border transition-colors ${
                selectedRoles.includes(role)
                  ? 'bg-brand-100 border-brand-300 text-brand-700 dark:bg-brand-900 dark:border-brand-700 dark:text-brand-300'
                  : 'bg-gray-50 border-gray-200 text-gray-600 hover:bg-gray-100 dark:bg-gray-800 dark:border-gray-600 dark:text-gray-400'
              }`}
            >
              {role}
            </button>
          ))}
        </div>
        {errors.roles && <p className="text-sm text-red-600">{errors.roles.message}</p>}
      </div>
      <Button type="submit" loading={isSubmitting}>{isEdit ? 'Update User' : 'Create User'}</Button>
    </form>
  );
}
