import { RetentionSettings } from '@/components/features/settings/RetentionSettings';
import { SavedFiltersList } from '@/components/features/settings/SavedFiltersList';

export default function SettingsPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Settings</h1>
      <RetentionSettings />
      <SavedFiltersList />
    </div>
  );
}
