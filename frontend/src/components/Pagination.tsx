import { ChevronLeft, ChevronRight } from 'lucide-react';

export function Pagination({ page, totalPages, onChange }: {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}) {
  return (
    <div className="flex items-center justify-between border-t border-line px-4 py-3 sm:px-6">
      <button className="button-ghost" disabled={page <= 1} onClick={() => onChange(page - 1)} type="button">
        <ChevronLeft size={15} /> Previous
      </button>
      <span className="number text-xs text-slate-500">{page} / {Math.max(totalPages, 1)}</span>
      <button className="button-ghost" disabled={page >= totalPages} onClick={() => onChange(page + 1)} type="button">
        Next <ChevronRight size={15} />
      </button>
    </div>
  );
}
