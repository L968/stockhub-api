import { CandlestickChart } from 'lucide-react';

export function Brand({ compact = false }: { compact?: boolean }) {
  return (
    <div className="flex items-center gap-3">
      <span className="grid h-10 w-10 place-items-center rounded-xl border border-lime/30 bg-lime/10 text-lime shadow-glow">
        <CandlestickChart size={21} aria-hidden="true" />
      </span>
      {!compact && (
        <span>
          <strong className="block text-[15px] font-semibold tracking-tight text-white">StockHub</strong>
          <span className="block font-mono text-[10px] uppercase tracking-[0.18em] text-slate-500">Market lab</span>
        </span>
      )}
    </div>
  );
}
