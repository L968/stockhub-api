import type { PortfolioResponse } from '../../types';

interface PortfolioCardProps {
  portfolio: PortfolioResponse;
}

export default function PortfolioCard({ portfolio }: PortfolioCardProps) {
  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  return (
    <div className="bg-slate-800 rounded-xl shadow-lg border border-slate-700 p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-bold text-slate-100">Portfolio</h2>
        <div className="text-right">
          <p className="text-sm text-slate-400">Total Value</p>
          <p className="text-2xl font-bold text-cyan-400">{formatCurrency(portfolio.totalValue)}</p>
        </div>
      </div>

      {portfolio.positions.length === 0 ? (
        <div className="text-center py-8 text-slate-400">
          <p>No positions yet</p>
          <p className="text-sm mt-1">Start trading to build your portfolio</p>
        </div>
      ) : (
        <div className="space-y-3">
          {portfolio.positions.map((position, index) => {
            const profitLoss = position.marketValue - (position.quantity * position.avgPrice);
            const profitLossPercent = position.avgPrice > 0 
              ? ((position.currentPrice - position.avgPrice) / position.avgPrice) * 100 
              : 0;

            return (
              <div
                key={`${position.symbol}-${index}`}
                className="flex items-center justify-between p-4 bg-slate-700/50 rounded-lg hover:bg-slate-700 border border-slate-600/50 transition"
              >
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <h3 className="font-semibold text-slate-100">{position.symbol}</h3>
                    {profitLoss !== 0 && (
                      <span className={`text-xs font-medium ${
                        profitLoss >= 0 ? 'text-emerald-400' : 'text-rose-400'
                      }`}>
                        {profitLoss >= 0 ? '+' : ''}{profitLossPercent.toFixed(2)}%
                      </span>
                    )}
                  </div>
                  <p className="text-sm text-slate-300 mt-1">
                    {position.quantity} shares @ {formatCurrency(position.avgPrice)}
                  </p>
                  <p className="text-xs text-slate-400 mt-1">
                    Current: {formatCurrency(position.currentPrice)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="font-semibold text-slate-100">{formatCurrency(position.marketValue)}</p>
                  {profitLoss !== 0 && (
                    <p className={`text-xs mt-1 ${
                      profitLoss >= 0 ? 'text-emerald-400' : 'text-rose-400'
                    }`}>
                      {profitLoss >= 0 ? '+' : ''}{formatCurrency(profitLoss)}
                    </p>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
