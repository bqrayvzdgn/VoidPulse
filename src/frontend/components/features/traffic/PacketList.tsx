'use client';

import { cn } from '@/lib/utils';
import type { CapturedPacketResponse } from '@/types/traffic';

interface PacketListProps {
  packets: CapturedPacketResponse[];
  selectedId?: string;
  onSelect: (packet: CapturedPacketResponse) => void;
  loading?: boolean;
}

function formatTime(capturedAt: string): string {
  const d = new Date(capturedAt);
  const hh = d.getHours().toString().padStart(2, '0');
  const mm = d.getMinutes().toString().padStart(2, '0');
  const ss = d.getSeconds().toString().padStart(2, '0');
  const ms = d.getMilliseconds().toString().padStart(3, '0');
  return `${hh}:${mm}:${ss}.${ms}`;
}

function getRowColor(packet: CapturedPacketResponse): string {
  const info = packet.info?.toLowerCase() ?? '';
  const protocol = packet.protocol?.toUpperCase() ?? '';

  if (info.includes('tls')) {
    return 'bg-green-50 dark:bg-green-900/20';
  }
  if (info.includes('http')) {
    return 'bg-emerald-50 dark:bg-emerald-900/20';
  }
  if (packet.sourcePort === 53 || packet.destinationPort === 53) {
    return 'bg-sky-50 dark:bg-sky-900/20';
  }
  if (info.includes('rst')) {
    return 'bg-red-50 dark:bg-red-900/20';
  }
  if (info.includes('syn')) {
    return 'bg-gray-100 dark:bg-gray-700/30';
  }
  if (protocol === 'UDP') {
    return 'bg-blue-50 dark:bg-blue-900/20';
  }
  // TCP default
  return 'bg-white dark:bg-gray-900';
}

function getTextColor(packet: CapturedPacketResponse): string {
  const info = packet.info?.toLowerCase() ?? '';
  const protocol = packet.protocol?.toUpperCase() ?? '';

  if (info.includes('tls')) return 'text-green-900 dark:text-green-200';
  if (info.includes('http')) return 'text-emerald-900 dark:text-emerald-200';
  if (packet.sourcePort === 53 || packet.destinationPort === 53) return 'text-sky-900 dark:text-sky-200';
  if (info.includes('rst')) return 'text-red-900 dark:text-red-200';
  if (info.includes('syn')) return 'text-gray-700 dark:text-gray-300';
  if (protocol === 'UDP') return 'text-blue-900 dark:text-blue-200';
  return 'text-gray-900 dark:text-gray-100';
}

const COLUMNS = [
  { key: 'no', header: 'No.', width: 'w-14' },
  { key: 'time', header: 'Time', width: 'w-28' },
  { key: 'source', header: 'Source', width: 'w-36' },
  { key: 'destination', header: 'Destination', width: 'w-36' },
  { key: 'protocol', header: 'Protocol', width: 'w-20' },
  { key: 'length', header: 'Length', width: 'w-16' },
  { key: 'info', header: 'Info', width: 'flex-1' },
] as const;

export function PacketList({ packets, selectedId, onSelect, loading }: PacketListProps) {
  if (loading) {
    return (
      <div className="flex items-center justify-center h-full text-sm text-gray-500 dark:text-gray-400">
        Loading packets...
      </div>
    );
  }

  if (packets.length === 0) {
    return (
      <div className="flex items-center justify-center h-full text-sm text-gray-500 dark:text-gray-400">
        No packets captured
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col overflow-hidden bg-white dark:bg-gray-950">
      {/* Header */}
      <div className="flex items-center border-b border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-800 px-1 shrink-0">
        {COLUMNS.map((col) => (
          <div
            key={col.key}
            className={cn(
              'px-1.5 py-1 text-xs font-semibold text-gray-600 dark:text-gray-300 select-none',
              col.width
            )}
          >
            {col.header}
          </div>
        ))}
      </div>

      {/* Rows */}
      <div className="flex-1 overflow-auto">
        {packets.map((packet, index) => {
          const isSelected = selectedId === packet.id;
          const rowBg = getRowColor(packet);
          const rowText = getTextColor(packet);

          return (
            <button
              key={packet.id}
              type="button"
              onClick={() => onSelect(packet)}
              className={cn(
                'flex items-center w-full px-1 text-left font-mono text-xs',
                'border-b border-gray-100 dark:border-gray-800',
                'hover:brightness-95 dark:hover:brightness-110 transition-all',
                rowBg,
                rowText,
                isSelected && 'ring-2 ring-blue-500 ring-inset z-10 relative'
              )}
            >
              <div className="w-14 px-1.5 py-0.5 tabular-nums">{index + 1}</div>
              <div className="w-28 px-1.5 py-0.5 tabular-nums">{formatTime(packet.capturedAt)}</div>
              <div className="w-36 px-1.5 py-0.5 truncate">{packet.sourceIp}</div>
              <div className="w-36 px-1.5 py-0.5 truncate">{packet.destinationIp}</div>
              <div className="w-20 px-1.5 py-0.5">{packet.protocol}</div>
              <div className="w-16 px-1.5 py-0.5 tabular-nums">{packet.packetLength}</div>
              <div className="flex-1 px-1.5 py-0.5 truncate">{packet.info}</div>
            </button>
          );
        })}
      </div>
    </div>
  );
}
