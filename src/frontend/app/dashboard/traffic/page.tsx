import { TrafficList } from '@/components/features/traffic/TrafficList';

export default function TrafficPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Traffic Flows</h1>
      <TrafficList />
    </div>
  );
}
