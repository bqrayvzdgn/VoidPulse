import { ApiResponse } from '@/types/api';
import { AuthResponse } from '@/types/auth';
import { API_BASE } from './constants';
import { getAccessToken, getRefreshToken, setTokens, redirectToLogin } from './auth';

let isRefreshing = false;
let refreshPromise: Promise<boolean> | null = null;

async function refreshAccessToken(): Promise<boolean> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) return false;

  try {
    const res = await fetch(`${API_BASE}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });

    if (!res.ok) return false;

    const data: ApiResponse<AuthResponse> = await res.json();
    if (data.success && data.data) {
      setTokens(data.data.accessToken, data.data.refreshToken);
      return true;
    }
    return false;
  } catch {
    return false;
  }
}

async function fetchApi<T>(
  endpoint: string,
  options?: RequestInit
): Promise<ApiResponse<T>> {
  const token = getAccessToken();
  const res = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options?.headers,
    },
  });

  if (res.status === 401) {
    if (!isRefreshing) {
      isRefreshing = true;
      refreshPromise = refreshAccessToken();
    }

    const refreshed = await refreshPromise;
    isRefreshing = false;
    refreshPromise = null;

    if (refreshed) {
      return fetchApi<T>(endpoint, options);
    }
    redirectToLogin();
    return { success: false, data: null, error: { code: 'UNAUTHORIZED', message: 'Session expired', details: null }, meta: null };
  }

  return res.json();
}

export const api = {
  get: <T>(url: string) => fetchApi<T>(url),
  post: <T>(url: string, data?: unknown) =>
    fetchApi<T>(url, { method: 'POST', body: data ? JSON.stringify(data) : undefined }),
  put: <T>(url: string, data?: unknown) =>
    fetchApi<T>(url, { method: 'PUT', body: data ? JSON.stringify(data) : undefined }),
  delete: <T>(url: string) =>
    fetchApi<T>(url, { method: 'DELETE' }),
};
