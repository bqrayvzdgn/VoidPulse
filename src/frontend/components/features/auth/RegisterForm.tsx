'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '@/providers/AuthProvider';
import { useToast } from '@/providers/ToastProvider';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';

const schema = z.object({
  fullName: z.string().min(1, 'Full name is required').max(200),
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  confirmPassword: z.string(),
  tenantName: z.string().min(1, 'Organization name is required').max(200),
  tenantSlug: z.string().min(1, 'Organization slug is required').max(100)
    .regex(/^[a-z0-9-]+$/, 'Slug must be lowercase letters, numbers, and hyphens'),
}).refine((data) => data.password === data.confirmPassword, {
  message: 'Passwords do not match',
  path: ['confirmPassword'],
});

type FormData = z.infer<typeof schema>;

export function RegisterForm() {
  const { register: registerUser } = useAuth();
  const { addToast } = useToast();
  const router = useRouter();
  const [serverError, setServerError] = useState('');

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const onSubmit = async (data: FormData) => {
    setServerError('');
    const result = await registerUser({
      email: data.email,
      password: data.password,
      fullName: data.fullName,
      tenantName: data.tenantName,
      tenantSlug: data.tenantSlug,
    });
    if (result.success) {
      addToast('success', 'Account created successfully');
      router.push('/dashboard');
    } else {
      setServerError(result.error?.message || 'Registration failed');
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {serverError && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/50 dark:text-red-300">
          {serverError}
        </div>
      )}
      <Input label="Full Name" autoComplete="name" error={errors.fullName?.message} {...register('fullName')} />
      <Input label="Email" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
      <Input label="Password" type="password" autoComplete="new-password" error={errors.password?.message} {...register('password')} />
      <Input label="Confirm Password" type="password" autoComplete="new-password" error={errors.confirmPassword?.message} {...register('confirmPassword')} />
      <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
        <p className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Organization Details</p>
        <div className="space-y-4">
          <Input label="Organization Name" error={errors.tenantName?.message} {...register('tenantName')} />
          <Input label="Organization Slug" placeholder="my-org" error={errors.tenantSlug?.message} {...register('tenantSlug')} />
        </div>
      </div>
      <Button type="submit" loading={isSubmitting} className="w-full">Create Account</Button>
    </form>
  );
}
