export interface AgentKeyResponse {
  id: string;
  name: string;
  apiKey: string | null;
  isActive: boolean;
  lastUsedAt: string | null;
  createdAt: string;
}

export interface CreateAgentKeyRequest {
  name: string;
}

export interface UpdateAgentKeyRequest {
  name?: string;
  isActive?: boolean;
}
