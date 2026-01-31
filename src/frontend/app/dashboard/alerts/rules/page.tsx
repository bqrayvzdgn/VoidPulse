'use client';

import { useState } from 'react';
import { useAlertRules, useCreateAlertRule, useUpdateAlertRule, useDeleteAlertRule } from '@/hooks/useAlerts';
import { Spinner } from '@/components/ui/Spinner';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { ALERT_CONDITIONS, ALERT_CONDITION_LABELS, ALERT_SEVERITIES, ALERT_SEVERITY_LABELS } from '@/types/alerts';
import type { CreateAlertRuleRequest } from '@/types/alerts';
import { cn } from '@/lib/utils';

const severityBadge: Record<number, string> = {
  0: 'bg-blue-100 text-blue-800 dark:bg-blue-900/50 dark:text-blue-300',
  1: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300',
  2: 'bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-300',
};

export default function AlertRulesPage() {
  const { data: rules, isLoading } = useAlertRules();
  const createMutation = useCreateAlertRule();
  const updateMutation = useUpdateAlertRule();
  const deleteMutation = useDeleteAlertRule();

  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<CreateAlertRuleRequest>({
    name: '',
    description: '',
    condition: 0,
    thresholdJson: '{"MaxBytes": 10000000}',
    severity: 1,
  });

  const handleCreate = () => {
    createMutation.mutate(form, {
      onSuccess: () => {
        setShowForm(false);
        setForm({ name: '', description: '', condition: 0, thresholdJson: '{"MaxBytes": 10000000}', severity: 1 });
      },
    });
  };

  const handleToggle = (ruleId: string, isEnabled: boolean) => {
    updateMutation.mutate({ ruleId, data: { isEnabled: !isEnabled } });
  };

  const handleDelete = (ruleId: string) => {
    if (confirm('Are you sure you want to delete this rule?')) {
      deleteMutation.mutate(ruleId);
    }
  };

  const thresholdPlaceholders: Record<number, string> = {
    0: '{"MaxBytes": 10000000}',
    1: '{}',
    2: '{"MaxDistinctPorts": 20, "TimeWindowSeconds": 60}',
    3: '{}',
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Alert Rules</h1>
        <Button variant="primary" size="sm" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : 'Create Rule'}
        </Button>
      </div>

      {showForm && (
        <div className="rounded-lg border border-gray-200 bg-white p-6 dark:border-gray-700 dark:bg-gray-800">
          <h2 className="mb-4 text-lg font-semibold text-gray-900 dark:text-gray-100">New Alert Rule</h2>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Name</label>
              <Input value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} placeholder="Rule name" />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Description</label>
              <Input value={form.description || ''} onChange={e => setForm({ ...form, description: e.target.value })} placeholder="Optional description" />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Condition</label>
              <Select
                options={ALERT_CONDITIONS.map(c => ({ label: c.label, value: String(c.value) }))}
                value={String(form.condition)}
                onChange={e => {
                  const cond = Number(e.target.value);
                  setForm({ ...form, condition: cond, thresholdJson: thresholdPlaceholders[cond] || '{}' });
                }}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Severity</label>
              <Select
                options={ALERT_SEVERITIES.map(s => ({ label: s.label, value: String(s.value) }))}
                value={String(form.severity)}
                onChange={e => setForm({ ...form, severity: Number(e.target.value) })}
              />
            </div>
            <div className="md:col-span-2">
              <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">Threshold (JSON)</label>
              <Input
                value={form.thresholdJson}
                onChange={e => setForm({ ...form, thresholdJson: e.target.value })}
                placeholder='{"MaxBytes": 10000000}'
              />
            </div>
          </div>
          <div className="mt-4 flex justify-end">
            <Button variant="primary" size="sm" onClick={handleCreate} disabled={!form.name || createMutation.isPending}>
              Create
            </Button>
          </div>
        </div>
      )}

      {isLoading ? (
        <Spinner className="py-12" />
      ) : (
        <div className="overflow-hidden rounded-lg border border-gray-200 dark:border-gray-700">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Name</th>
                <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Condition</th>
                <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Severity</th>
                <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Status</th>
                <th className="px-6 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 bg-white dark:divide-gray-700 dark:bg-gray-900">
              {rules && rules.length > 0 ? (
                rules.map(rule => (
                  <tr key={rule.id} className="hover:bg-gray-50 dark:hover:bg-gray-800">
                    <td className="px-6 py-4">
                      <div className="text-sm font-medium text-gray-900 dark:text-gray-100">{rule.name}</div>
                      {rule.description && (
                        <div className="text-xs text-gray-500 dark:text-gray-400">{rule.description}</div>
                      )}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm text-gray-600 dark:text-gray-400">
                      {ALERT_CONDITION_LABELS[rule.condition]}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <span className={cn('inline-flex rounded-full px-2 py-0.5 text-xs font-medium', severityBadge[rule.severity])}>
                        {ALERT_SEVERITY_LABELS[rule.severity]}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm">
                      <button
                        onClick={() => handleToggle(rule.id, rule.isEnabled)}
                        className={cn(
                          'inline-flex rounded-full px-2 py-0.5 text-xs font-medium',
                          rule.isEnabled
                            ? 'bg-green-100 text-green-800 dark:bg-green-900/50 dark:text-green-300'
                            : 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400'
                        )}
                      >
                        {rule.isEnabled ? 'Enabled' : 'Disabled'}
                      </button>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-right">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDelete(rule.id)}
                        disabled={deleteMutation.isPending}
                        className="text-red-600 hover:text-red-700 dark:text-red-400"
                      >
                        Delete
                      </Button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-sm text-gray-500 dark:text-gray-400">
                    No alert rules configured. Create one to start monitoring.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
