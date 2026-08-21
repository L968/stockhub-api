import type { LucideIcon } from 'lucide-react';
import { AlertCircle, Loader2, RefreshCw } from 'lucide-react';

export function LoadingBlock({ label = 'Loading data' }: { label?: string }) {
  return (
    <div className="panel grid min-h-48 place-items-center" role="status">
      <div className="flex items-center gap-3 text-sm text-slate-400">
        <Loader2 className="animate-spin text-lime" size={18} />
        {label}
      </div>
    </div>
  );
}

export function ErrorBlock({ message, retry }: { message: string; retry?: () => void }) {
  return (
    <div className="panel flex min-h-48 flex-col items-center justify-center gap-4 px-6 text-center" role="alert">
      <AlertCircle className="text-rose-400" size={24} />
      <div>
        <p className="font-medium text-slate-200">Something went wrong</p>
        <p className="mt-1 text-sm text-slate-500">{message}</p>
      </div>
      {retry && (
        <button className="button-secondary" onClick={retry} type="button">
          <RefreshCw size={14} /> Try again
        </button>
      )}
    </div>
  );
}

export function EmptyState({ icon: Icon, title, description }: {
  icon: LucideIcon;
  title: string;
  description: string;
}) {
  return (
    <div className="grid min-h-44 place-items-center px-6 text-center">
      <div>
        <Icon className="mx-auto text-slate-600" size={26} />
        <p className="mt-3 font-medium text-slate-300">{title}</p>
        <p className="mt-1 text-sm text-slate-500">{description}</p>
      </div>
    </div>
  );
}
