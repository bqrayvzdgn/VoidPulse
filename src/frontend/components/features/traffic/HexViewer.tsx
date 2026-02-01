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
      <pre className="font-mono text-xs leading-5 select-text whitespace-pre">
        {lines.map((line) => {
          // Build hex part as array of spans
          const hexParts: React.ReactNode[] = [];
          for (let i = 0; i < 16; i++) {
            const byteIndex = line.offset + i;
            if (i < line.bytes.length) {
              const highlighted = isHighlighted(byteIndex);
              hexParts.push(
                <span
                  key={`h${i}`}
                  className={cn(
                    highlighted && 'bg-blue-200 dark:bg-blue-800 rounded-sm'
                  )}
                >
                  {toHex(line.bytes[i])}
                </span>
              );
            } else {
              hexParts.push(<span key={`h${i}`}>{'  '}</span>);
            }
            // Separator: double space after byte 7, single space otherwise
            if (i < 15) {
              hexParts.push(<span key={`s${i}`}>{i === 7 ? '  ' : ' '}</span>);
            }
          }

          // Build ASCII part
          const asciiParts = Array.from(line.bytes).map((byte, i) => {
            const byteIndex = line.offset + i;
            const highlighted = isHighlighted(byteIndex);
            return (
              <span
                key={`a${i}`}
                className={cn(
                  highlighted && 'bg-blue-200 dark:bg-blue-800 rounded-sm'
                )}
              >
                {toAscii(byte)}
              </span>
            );
          });

          const offset = line.offset.toString(16).padStart(4, '0');

          return (
            <div key={line.offset}>
              <span className="text-gray-400 dark:text-gray-500 select-none">{offset}</span>
              {'  '}
              {hexParts}
              {'  '}
              <span className="text-gray-600 dark:text-gray-400">{asciiParts}</span>
              {'\n'}
            </div>
          );
        })}
      </pre>
    </div>
  );
}
