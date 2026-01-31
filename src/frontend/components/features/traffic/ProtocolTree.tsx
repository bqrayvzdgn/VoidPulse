'use client';

import { useState } from 'react';
import { cn } from '@/lib/utils';
import type { ProtocolLayer } from '@/types/traffic';

interface ProtocolTreeProps {
  layers: ProtocolLayer[];
  onFieldSelect?: (offset: number, length: number) => void;
}

function LayerSection({
  layer,
  onFieldSelect,
}: {
  layer: ProtocolLayer;
  onFieldSelect?: (offset: number, length: number) => void;
}) {
  const [expanded, setExpanded] = useState(true);
  const fieldEntries = Object.entries(layer.fields);
  const summary = fieldEntries.length > 0
    ? fieldEntries.slice(0, 3).map(([, v]) => v).join(', ')
    : '';

  return (
    <div className="border-b border-gray-200 dark:border-gray-700 last:border-b-0">
      <button
        type="button"
        onClick={() => setExpanded(!expanded)}
        className={cn(
          'flex items-center gap-1.5 w-full px-2 py-1 text-left text-xs',
          'hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors'
        )}
      >
        <span className="text-gray-400 dark:text-gray-500 w-4 shrink-0 text-center select-none">
          {expanded ? '\u25BC' : '\u25B6'}
        </span>
        <span className="font-bold text-gray-900 dark:text-gray-100">
          {layer.name}
        </span>
        {!expanded && summary && (
          <span className="text-gray-500 dark:text-gray-400 truncate ml-1">
            {summary}
          </span>
        )}
      </button>

      {expanded && fieldEntries.length > 0 && (
        <div className="pl-7 pb-1">
          {fieldEntries.map(([key, value]) => (
            <button
              key={key}
              type="button"
              onClick={() => onFieldSelect?.(layer.offset, layer.length)}
              className={cn(
                'flex items-baseline gap-2 w-full px-2 py-0.5 text-left text-xs',
                'hover:bg-blue-50 dark:hover:bg-blue-900/30 rounded transition-colors',
                onFieldSelect && 'cursor-pointer'
              )}
            >
              <span className="text-gray-600 dark:text-gray-400 shrink-0">
                {key}:
              </span>
              <span className="font-mono text-gray-500 dark:text-gray-400 truncate">
                {value}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export function ProtocolTree({ layers, onFieldSelect }: ProtocolTreeProps) {
  if (layers.length === 0) {
    return (
      <div className="flex items-center justify-center h-full text-sm text-gray-500 dark:text-gray-400">
        No protocol layers
      </div>
    );
  }

  return (
    <div className="h-full overflow-auto bg-white dark:bg-gray-950">
      {layers.map((layer, i) => (
        <LayerSection key={`${layer.name}-${i}`} layer={layer} onFieldSelect={onFieldSelect} />
      ))}
    </div>
  );
}
