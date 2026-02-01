function getApiBase(): string {
  // Server-side rendering: use env var or internal Docker URL
  if (typeof window === 'undefined') {
    return process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080/api/v1';
  }

  // Client-side: if env var was set at build time, use it
  if (process.env.NEXT_PUBLIC_API_URL) {
    return process.env.NEXT_PUBLIC_API_URL;
  }

  // Client-side fallback: derive from current browser URL
  // Backend runs on same host, port 8080
  return `${window.location.protocol}//${window.location.hostname}:8080/api/v1`;
}

export const API_BASE = getApiBase();

export const ROLES = {
  SUPER_ADMIN: 'SuperAdmin',
  TENANT_ADMIN: 'TenantAdmin',
  ANALYST: 'Analyst',
  VIEWER: 'Viewer',
} as const;

export const PROTOCOLS = ['TCP', 'UDP', 'ICMP'] as const;

export const PERIOD_OPTIONS = [
  { label: 'Last Hour', value: '1h' },
  { label: 'Last 24 Hours', value: '24h' },
  { label: 'Last 7 Days', value: '7d' },
  { label: 'Last 30 Days', value: '30d' },
] as const;

export const DEFAULT_PAGE_SIZE = 20;
export const MAX_PAGE_SIZE = 100;
