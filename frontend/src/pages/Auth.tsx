import { useState } from 'react';
import { TrendingUp } from 'lucide-react';
import LoginForm from '../components/Auth/LoginForm';
import SignUpForm from '../components/Auth/SignUpForm';

export default function Auth() {
  const [mode, setMode] = useState<'login' | 'signup'>('login');

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 flex items-center justify-center p-4">
      <div className="w-full max-w-6xl grid lg:grid-cols-2 gap-12 items-center">
        <div className="hidden lg:block">
          <div className="text-center">
            <div className="w-20 h-20 bg-gradient-to-br from-cyan-500 to-violet-500 rounded-2xl flex items-center justify-center mx-auto mb-6 shadow-lg shadow-cyan-500/30">
              <TrendingUp className="w-12 h-12 text-white" />
            </div>
            <h1 className="text-5xl font-bold text-slate-100 mb-4">StockHub</h1>
            <p className="text-xl text-slate-300 mb-8">
              Professional Trading Platform
            </p>
            <div className="space-y-4 text-left max-w-md mx-auto">
              <div className="flex items-start gap-3">
                <div className="w-8 h-8 bg-cyan-500/20 rounded-lg flex items-center justify-center flex-shrink-0 mt-1 border border-cyan-500/30">
                  <TrendingUp className="w-5 h-5 text-cyan-400" />
                </div>
                <div>
                  <h3 className="font-semibold text-slate-100">Real-time Trading</h3>
                  <p className="text-sm text-slate-400">
                    Execute trades instantly with live market data
                  </p>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <div className="w-8 h-8 bg-emerald-500/20 rounded-lg flex items-center justify-center flex-shrink-0 mt-1 border border-emerald-500/30">
                  <TrendingUp className="w-5 h-5 text-emerald-400" />
                </div>
                <div>
                  <h3 className="font-semibold text-slate-100">Portfolio Management</h3>
                  <p className="text-sm text-slate-400">
                    Track your investments and performance
                  </p>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <div className="w-8 h-8 bg-violet-500/20 rounded-lg flex items-center justify-center flex-shrink-0 mt-1 border border-violet-500/30">
                  <TrendingUp className="w-5 h-5 text-violet-400" />
                </div>
                <div>
                  <h3 className="font-semibold text-slate-100">Advanced Analytics</h3>
                  <p className="text-sm text-slate-400">
                    Make informed decisions with detailed insights
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="flex justify-center">
          {mode === 'login' ? (
            <LoginForm onToggleMode={() => setMode('signup')} />
          ) : (
            <SignUpForm onToggleMode={() => setMode('login')} />
          )}
        </div>
      </div>
    </div>
  );
}
