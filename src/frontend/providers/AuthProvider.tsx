'use client';

import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import { getAccessToken, getStoredUser, setTokens, setStoredUser, clearTokens } from '@/lib/auth';
import type { UserInfo, LoginRequest, RegisterRequest, AuthResponse } from '@/types/auth';
import type { ApiResponse } from '@/types/api';

interface AuthContextType {
  user: UserInfo | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (data: LoginRequest) => Promise<ApiResponse<AuthResponse>>;
  register: (data: RegisterRequest) => Promise<ApiResponse<AuthResponse>>;
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
  hasAnyRole: (...roles: string[]) => boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const token = getAccessToken();
    const storedUser = getStoredUser();
    if (token && storedUser) {
      setUser(storedUser);
    }
    setIsLoading(false);
  }, []);

  const handleAuthSuccess = useCallback((data: AuthResponse) => {
    setTokens(data.accessToken, data.refreshToken);
    setStoredUser(data.user);
    setUser(data.user);
  }, []);

  const login = useCallback(async (data: LoginRequest): Promise<ApiResponse<AuthResponse>> => {
    const response = await api.post<AuthResponse>('/auth/login', data);
    if (response.success && response.data) {
      handleAuthSuccess(response.data);
    }
    return response;
  }, [handleAuthSuccess]);

  const register = useCallback(async (data: RegisterRequest): Promise<ApiResponse<AuthResponse>> => {
    const response = await api.post<AuthResponse>('/auth/register', data);
    if (response.success && response.data) {
      handleAuthSuccess(response.data);
    }
    return response;
  }, [handleAuthSuccess]);

  const logout = useCallback(async () => {
    try {
      await api.delete('/auth/logout');
    } catch {
      // Ignore errors on logout
    } finally {
      clearTokens();
      setUser(null);
      router.push('/login');
    }
  }, [router]);

  const hasRole = useCallback((role: string) => {
    return user?.roles.includes(role) ?? false;
  }, [user]);

  const hasAnyRole = useCallback((...roles: string[]) => {
    return roles.some(role => user?.roles.includes(role)) ?? false;
  }, [user]);

  return (
    <AuthContext.Provider value={{
      user,
      isLoading,
      isAuthenticated: !!user,
      login,
      register,
      logout,
      hasRole,
      hasAnyRole,
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
