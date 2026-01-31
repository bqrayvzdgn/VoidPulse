'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useFlowPackets } from '@/hooks/usePackets';
import { PacketList } from '@/components/features/traffic/PacketList';
import { ProtocolTree } from '@/components/features/traffic/ProtocolTree';
import { HexViewer } from '@/components/features/traffic/HexViewer';
import type { CapturedPacketResponse } from '@/types/traffic';

const PAGE_SIZE = 50;

export default function PacketInspectorPage() {
  const params = useParams<{ id: string }>();
  const flowId = params.id;

  const [page, setPage] = useState(1);
  const [selectedPacket, setSelectedPacket] = useState<CapturedPacketResponse | null>(null);
  const [highlightRange, setHighlightRange] = useState<{ start: number; end: number } | undefined>(undefined);

  const { data, isLoading } = useFlowPackets(flowId, { page, pageSize: PAGE_SIZE });

  const packets = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;
  const totalCount = data?.totalCount ?? 0;

  function handleSelectPacket(packet: CapturedPacketResponse) {
    setSelectedPacket(packet);
    setHighlightRange(undefined);
  }

  function handleFieldSelect(offset: number, length: number) {
    setHighlightRange({ start: offset, end: offset + length });
  }

  return (
    <div className="flex flex-col h-full bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-2 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-950 shrink-0">
        <div className="flex items-center gap-3">
          <Link
            href={`/dashboard/traffic/${flowId}`}
            className="text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors"
          >
            &larr; Back
          </Link>
          <h1 className="text-sm font-semibold text-gray-900 dark:text-gray-100">
            Packet Inspector
          </h1>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {totalCount} packet{totalCount !== 1 ? 's' : ''}
          </span>
        </div>
      </div>

      {/* Top panel: Packet List */}
      <div className="flex-1 min-h-0 overflow-hidden">
        <PacketList
          packets={packets}
          selectedId={selectedPacket?.id}
          onSelect={handleSelectPacket}
          loading={isLoading}
        />
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 px-4 py-1.5 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-950 shrink-0">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            className="px-2 py-0.5 text-xs rounded border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Prev
          </button>
          <span className="text-xs text-gray-600 dark:text-gray-400">
            Page {page} of {totalPages}
          </span>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            className="px-2 py-0.5 text-xs rounded border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Next
          </button>
        </div>
      )}

      {/* Divider */}
      <div className="h-px bg-gray-300 dark:bg-gray-600 shrink-0" />

      {/* Middle panel: Protocol Tree */}
      <div className="h-48 shrink-0 overflow-hidden border-t border-gray-200 dark:border-gray-700">
        {selectedPacket ? (
          <ProtocolTree
            layers={selectedPacket.protocolStack}
            onFieldSelect={handleFieldSelect}
          />
        ) : (
          <div className="flex items-center justify-center h-full text-sm text-gray-500 dark:text-gray-400">
            Select a packet to view protocol details
          </div>
        )}
      </div>

      {/* Divider */}
      <div className="h-px bg-gray-300 dark:bg-gray-600 shrink-0" />

      {/* Bottom panel: Hex Viewer */}
      <div className="h-40 shrink-0 overflow-hidden border-t border-gray-200 dark:border-gray-700">
        {selectedPacket ? (
          <HexViewer
            data={selectedPacket.headerBytesBase64}
            highlightRange={highlightRange}
          />
        ) : (
          <div className="flex items-center justify-center h-full text-sm text-gray-500 dark:text-gray-400">
            Select a packet to view hex dump
          </div>
        )}
      </div>
    </div>
  );
}
