'use client';

import { useState } from 'react';
import { usePackets } from '@/hooks/usePackets';
import { PacketList } from '@/components/features/traffic/PacketList';
import { ProtocolTree } from '@/components/features/traffic/ProtocolTree';
import { HexViewer } from '@/components/features/traffic/HexViewer';
import type { CapturedPacketResponse, PacketQueryParams } from '@/types/traffic';

const PAGE_SIZE = 100;

export default function AllPacketsPage() {
  const [page, setPage] = useState(1);
  const [selectedPacket, setSelectedPacket] = useState<CapturedPacketResponse | null>(null);
  const [highlightRange, setHighlightRange] = useState<{ start: number; end: number } | undefined>(undefined);
  const [filters, setFilters] = useState<PacketQueryParams>({});

  const { data, isLoading } = usePackets({ ...filters, page, pageSize: PAGE_SIZE });

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

  function handleApplyFilters(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    setFilters({
      sourceIp: fd.get('sourceIp') as string || undefined,
      destinationIp: fd.get('destinationIp') as string || undefined,
      protocol: fd.get('protocol') as string || undefined,
      search: fd.get('search') as string || undefined,
    });
    setPage(1);
  }

  return (
    <div className="flex flex-col h-[calc(100vh-4rem)]">
      {/* Filter bar */}
      <form onSubmit={handleApplyFilters} className="flex items-center gap-2 px-4 py-2 border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-950 shrink-0 flex-wrap">
        <input
          name="search"
          placeholder="Filter expression..."
          defaultValue={filters.search ?? ''}
          className="flex-1 min-w-[200px] px-3 py-1.5 text-sm font-mono rounded border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
        <input
          name="sourceIp"
          placeholder="Source IP"
          defaultValue={filters.sourceIp ?? ''}
          className="w-36 px-2 py-1.5 text-xs font-mono rounded border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
        <input
          name="destinationIp"
          placeholder="Dest IP"
          defaultValue={filters.destinationIp ?? ''}
          className="w-36 px-2 py-1.5 text-xs font-mono rounded border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
        <select
          name="protocol"
          defaultValue={filters.protocol ?? ''}
          className="w-24 px-2 py-1.5 text-xs rounded border border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        >
          <option value="">All</option>
          <option value="TCP">TCP</option>
          <option value="UDP">UDP</option>
          <option value="ICMP">ICMP</option>
        </select>
        <button
          type="submit"
          className="px-3 py-1.5 text-xs font-medium rounded bg-indigo-600 text-white hover:bg-indigo-500 transition-colors"
        >
          Apply
        </button>
        <span className="text-xs text-gray-500 dark:text-gray-400 ml-auto">
          {totalCount} packet{totalCount !== 1 ? 's' : ''}
        </span>
      </form>

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
