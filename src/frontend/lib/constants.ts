export const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api/v1';

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
