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
  processName: string | null;
  resolvedHostname: string | null;
  tlsSni: string | null;
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

export interface CapturedPacketResponse {
  id: string;
  trafficFlowId: string | null;
  capturedAt: string;
  sourceIp: string;
  destinationIp: string;
  sourcePort: number;
  destinationPort: number;
  protocol: string;
  packetLength: number;
  headerBytesBase64: string;
  protocolStack: ProtocolLayer[];
  info: string;
}

export interface ProtocolLayer {
  name: string;
  offset: number;
  length: number;
  fields: Record<string, string>;
}

export interface PacketQueryParams {
  sourceIp?: string;
  destinationIp?: string;
  protocol?: string;
  startDate?: string;
  endDate?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
