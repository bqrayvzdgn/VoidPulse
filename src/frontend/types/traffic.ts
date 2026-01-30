export interface TrafficFlowResponse {
  id: string;
  sourceIp: string;
  destinationIp: string;
  sourcePort: number;
  destinationPort: number;
  protocol: string;
  bytesSent: number;
  bytesReceived: number;
  packetsSent: number;
  packetsReceived: number;
  startedAt: string;
  endedAt: string;
  flowDuration: number;
  httpMetadata: HttpMetadataResponse | null;
  createdAt: string;
}

export interface HttpMetadataResponse {
  method: string;
  host: string;
  path: string;
  statusCode: number;
  userAgent: string | null;
  contentType: string | null;
  responseTimeMs: number;
}

export interface TrafficQueryParams {
  sourceIp?: string;
  destinationIp?: string;
  protocol?: string;
  startDate?: string;
  endDate?: string;
  sortBy?: string;
  sortOrder?: string;
  page?: number;
  pageSize?: number;
}
