export type AlertCondition = 'ByteThreshold' | 'UnknownDestination' | 'PortScan' | 'ProtocolAnomaly';
export type AlertSeverity = 'Info' | 'Warning' | 'Critical';

export interface AlertRuleResponse {
  id: string;
  name: string;
  description: string | null;
  condition: number;
  thresholdJson: string;
  severity: number;
  isEnabled: boolean;
  createdAt: string;
}

export interface CreateAlertRuleRequest {
  name: string;
  description?: string;
  condition: number;
  thresholdJson: string;
  severity: number;
}

export interface UpdateAlertRuleRequest {
  name?: string;
  description?: string;
  condition?: number;
  thresholdJson?: string;
  severity?: number;
  isEnabled?: boolean;
}

export interface AlertResponse {
  id: string;
  alertRuleId: string | null;
  ruleName: string;
  message: string;
  severity: number;
  sourceIp: string | null;
  destinationIp: string | null;
  metadataJson: string | null;
  isAcknowledged: boolean;
  triggeredAt: string;
  acknowledgedAt: string | null;
}

export interface AlertCountResponse {
  unacknowledgedCount: number;
}

export const ALERT_CONDITIONS: { label: string; value: number }[] = [
  { label: 'Byte Threshold', value: 0 },
  { label: 'Unknown Destination', value: 1 },
  { label: 'Port Scan', value: 2 },
  { label: 'Protocol Anomaly', value: 3 },
];

export const ALERT_CONDITION_LABELS: Record<number, string> = {
  0: 'Byte Threshold',
  1: 'Unknown Destination',
  2: 'Port Scan',
  3: 'Protocol Anomaly',
};

export const ALERT_SEVERITIES: { label: string; value: number }[] = [
  { label: 'Info', value: 0 },
  { label: 'Warning', value: 1 },
  { label: 'Critical', value: 2 },
];

export const ALERT_SEVERITY_LABELS: Record<number, string> = {
  0: 'Info',
  1: 'Warning',
  2: 'Critical',
};
