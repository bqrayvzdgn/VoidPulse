import { AgentList } from '@/components/features/agents/AgentList';

export default function AgentsPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Agent Keys</h1>
      <p className="text-sm text-gray-600 dark:text-gray-400">
        Manage API keys for network agents that ingest traffic data into VoidPulse.
      </p>
      <AgentList />
    </div>
  );
}
