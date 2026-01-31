'use client';

import { useMemo } from 'react';
import { cn } from '@/lib/utils';

interface HexViewerProps {
  data: string;
  highlightRange?: { start: number; end: number };
}

function base64ToBytes(base64: string): Uint8Array {
  try {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes;
  } catch {
    return new Uint8Array(0);
  }
}

function toHex(byte: number): string {
  return byte.toString(16).padStart(2, '0');
}

function toAscii(byte: number): string {
  return byte >= 0x20 && byte <= 0x7e ? String.fromCharCode(byte) : '.';
}

export function HexViewer({ data, highlightRange }: HexViewerProps) {
  const bytes = useMemo(() => base64ToBytes(data), [data]);

  if (bytes.length === 0) {
    return (
      <div className="flex items-center justify-center h-full text-sm text-gray-500 dark:text-gray-400">
        No packet data
      </div>
    );
  }

  const lines: { offset: number; bytes: Uint8Array }[] = [];
  for (let i = 0; i < bytes.length; i += 16) {
    lines.push({ offset: i, bytes: bytes.slice(i, i + 16) });
  }

  const isHighlighted = (index: number): boolean => {
    if (!highlightRange) return false;
    return index >= highlightRange.start && index < highlightRange.end;
  };

  return (
    <div className="h-full overflow-auto bg-white dark:bg-gray-950 p-2">
      <pre className="font-mono text-xs leading-5 select-text">
        {lines.map((line) => (
          <div key={line.offset} className="flex">
            {/* Offset column */}
            <span className="text-gray-400 dark:text-gray-500 select-none w-12 shrink-0">
              {line.offset.toString(16).padStart(4, '0')}
            </span>
            <span className="select-none w-4 shrink-0">{' '}</span>

            {/* Hex bytes */}
            <span className="shrink-0" style={{ width: '24.5ch' }}>
              {Array.from({ length: 16 }, (_, i) => {
                const byteIndex = line.offset + i;
                if (i < line.bytes.length) {
                  const highlighted = isHighlighted(byteIndex);
                  return (
                    <span key={i}>
                      <span
                        className={cn(
                          highlighted && 'bg-blue-200 dark:bg-blue-800 rounded-sm'
                        )}
                      >
                        {toHex(line.bytes[i])}
                      </span>
                      {i === 7 ? '  ' : ' '}
                    </span>
                  );
                }
                return (
                  <span key={i}>
                    {'  '}
                    {i === 7 ? '  ' : ' '}
                  </span>
                );
              })}
            </span>
            <span className="select-none w-4 shrink-0">{' '}</span>

            {/* ASCII column */}
            <span className="text-gray-600 dark:text-gray-400">
              {Array.from(line.bytes).map((byte, i) => {
                const byteIndex = line.offset + i;
                const highlighted = isHighlighted(byteIndex);
                return (
                  <span
                    key={i}
                    className={cn(
                      highlighted && 'bg-blue-200 dark:bg-blue-800 rounded-sm'
                    )}
                  >
                    {toAscii(byte)}
                  </span>
                );
              })}
            </span>
          </div>
        ))}
      </pre>
    </div>
  );
}
