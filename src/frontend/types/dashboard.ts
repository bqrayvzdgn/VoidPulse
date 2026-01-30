export interface OverviewResponse {
  totalFlows: number;
  totalBytes: number;
  activeAgents: number;
  uniqueSourceIps: number;
  uniqueDestinationIps: number;
}

export interface TopTalkersResponse {
  entries: TalkerEntry[];
}

export interface TalkerEntry {
  ip: string;
  totalBytes: number;
  flowCount: number;
}

export interface ProtocolDistributionResponse {
  entries: ProtocolEntry[];
}

export interface ProtocolEntry {
  protocol: string;
  totalBytes: number;
  flowCount: number;
  percentage: number;
}

export interface BandwidthResponse {
  entries: BandwidthEntry[];
}

export interface BandwidthEntry {
  timestamp: string;
  bytesSent: number;
  bytesReceived: number;
  totalBytes: number;
}
