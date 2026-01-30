'use client';

import { useState } from 'react';
import { useAgentKeys, useCreateAgentKey, useDeleteAgentKey } from '@/hooks/useAgents';
import { useToast } from '@/providers/ToastProvider';
import { Table } from '@/components/ui/Table';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { Modal, ConfirmModal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { formatDate } from '@/lib/utils';
import type { AgentKeyResponse } from '@/types/agent';

export function AgentList() {
  const { data, isLoading } = useAgentKeys();
  const createAgent = useCreateAgentKey();
  const deleteAgent = useDeleteAgentKey();
  const { addToast } = useToast();

  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState('');
  const [createdKey, setCreatedKey] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AgentKeyResponse | null>(null);

  const handleCreate = async () => {
    if (!newName.trim()) return;
    const result = await createAgent.mutateAsync({ name: newName });
    if (result.success && result.data?.apiKey) {
      setCreatedKey(result.data.apiKey);
      addToast('success', 'Agent key created');
    } else {
      addToast('error', result.error?.message || 'Failed to create agent key');
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    const result = await deleteAgent.mutateAsync(deleteTarget.id);
    if (result.success) {
      addToast('success', 'Agent key revoked');
    } else {
      addToast('error', result.error?.message || 'Failed to revoke key');
    }
    setDeleteTarget(null);
  };

  const columns = [
    { key: 'name', header: 'Name', render: (a: AgentKeyResponse) => <span className="font-medium">{a.name}</span> },
    {
      key: 'status',
      header: 'Status',
      render: (a: AgentKeyResponse) => <Badge variant={a.isActive ? 'success' : 'danger'}>{a.isActive ? 'Active' : 'Revoked'}</Badge>,
    },
    {
      key: 'lastUsed',
      header: 'Last Used',
      render: (a: AgentKeyResponse) => <span className="text-xs">{a.lastUsedAt ? formatDate(a.lastUsedAt) : 'Never'}</span>,
    },
    {
      key: 'created',
      header: 'Created',
      render: (a: AgentKeyResponse) => <span className="text-xs">{formatDate(a.createdAt)}</span>,
    },
    {
      key: 'actions',
      header: '',
      render: (a: AgentKeyResponse) => (
        <button onClick={() => setDeleteTarget(a)} className="text-red-600 hover:text-red-700 text-xs font-medium">
          Revoke
        </button>
      ),
    },
  ];

  if (isLoading) return <Spinner className="py-12" />;

  return (
    <>
      <Table columns={columns} data={data ?? []} keyExtractor={(a) => a.id} emptyMessage="No agent keys found" />

      <Modal isOpen={showCreate} onClose={() => { setShowCreate(false); setNewName(''); setCreatedKey(null); }} title="Create Agent Key">
        {createdKey ? (
          <div className="space-y-4">
            <p className="text-sm text-gray-600 dark:text-gray-400">Copy this API key now. It will not be shown again.</p>
            <div className="rounded-md bg-gray-50 p-3 dark:bg-gray-900">
              <code className="text-sm break-all select-all">{createdKey}</code>
            </div>
            <Button onClick={() => { navigator.clipboard.writeText(createdKey); addToast('success', 'Copied to clipboard'); }}>
              Copy Key
            </Button>
          </div>
        ) : (
          <div className="space-y-4">
            <Input label="Agent Name" value={newName} onChange={e => setNewName(e.target.value)} placeholder="e.g. production-agent-01" />
            <div className="flex justify-end gap-3">
              <Button variant="secondary" onClick={() => setShowCreate(false)}>Cancel</Button>
              <Button onClick={handleCreate} loading={createAgent.isPending} disabled={!newName.trim()}>Create</Button>
            </div>
          </div>
        )}
      </Modal>

      <ConfirmModal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        title="Revoke Agent Key"
        message={`Are you sure you want to revoke "${deleteTarget?.name}"? This agent will no longer be able to ingest data.`}
        confirmLabel="Revoke"
        loading={deleteAgent.isPending}
      />

      <div className="mt-4">
        <Button onClick={() => setShowCreate(true)}>Create Agent Key</Button>
      </div>
    </>
  );
}
