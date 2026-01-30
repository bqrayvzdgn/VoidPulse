export interface RetentionPolicyResponse {
  id: string;
  retentionDays: number;
  createdAt: string;
  updatedAt: string;
}

export interface RetentionPolicyRequest {
  retentionDays: number;
}
