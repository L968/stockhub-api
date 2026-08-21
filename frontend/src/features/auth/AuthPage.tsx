import { ArrowRight, BarChart3, GitBranch, Radio, ShieldCheck } from 'lucide-react';
import { useState, type FormEvent } from 'react';
import { Brand } from '../../components/Brand';
import { useAuth } from './AuthContext';

export function AuthPage() {
  const [mode, setMode] = useState<'signin' | 'signup'>('signin');
  const [email, setEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const { signIn, signUp } = useAuth();

  async function submit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSubmitting(true);
    try {
      if (mode === 'signup') await signUp(email, fullName);
      else await signIn(email);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Unable to continue.');
    } finally {
      setSubmitting(false);
    }
  }

  function changeMode() {
    setMode((value) => value === 'signin' ? 'signup' : 'signin');
    setError('');
  }

  const highlights = [
    { icon: Radio, label: 'Live order book' },
    { icon: GitBranch, label: 'Partitioned matching' },
    { icon: BarChart3, label: 'Portfolio tracking' },
    { icon: ShieldCheck, label: 'Safe simulation' },
  ];

  return (
    <main className="grid min-h-screen bg-ink lg:grid-cols-[1.1fr_0.9fr]">
      <section className="relative hidden overflow-hidden border-r border-line p-12 lg:flex lg:flex-col lg:justify-between">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_30%,rgba(182,243,107,0.10),transparent_35%),radial-gradient(circle_at_80%_70%,rgba(56,189,248,0.08),transparent_35%)]" />
        <div className="relative"><Brand /></div>
        <div className="relative max-w-xl">
          <p className="eyebrow">Event-driven trading playground</p>
          <h1 className="mt-5 text-5xl font-semibold leading-[1.05] tracking-[-0.04em] text-white xl:text-6xl">
            See the market.<br /><span className="text-lime">Test the engine.</span>
          </h1>
          <p className="mt-6 max-w-lg text-lg leading-relaxed text-slate-400">
            A realistic exchange simulation built to explore order books, asynchronous processing and scalable matching.
          </p>
          <div className="mt-10 grid grid-cols-2 gap-3">
            {highlights.map(({ icon: Icon, label }) => (
              <div className="flex items-center gap-3 rounded-xl border border-line bg-panel/50 p-4" key={label}>
                <Icon className="text-lime" size={17} />
                <span className="text-sm text-slate-300">{label}</span>
              </div>
            ))}
          </div>
        </div>
        <p className="relative font-mono text-[11px] text-slate-600">STOCKHUB / PORTFOLIO PROJECT</p>
      </section>

      <section className="flex items-center justify-center px-5 py-12">
        <div className="w-full max-w-md">
          <div className="mb-12 lg:hidden"><Brand /></div>
          <p className="eyebrow">{mode === 'signin' ? 'Welcome back' : 'Create your account'}</p>
          <h2 className="mt-3 text-3xl font-semibold tracking-tight text-white">
            {mode === 'signin' ? 'Enter the market' : 'Start your simulation'}
          </h2>
          <p className="mt-2 text-sm text-slate-500">
            Authentication is intentionally lightweight for this portfolio project.
          </p>

          <form className="mt-8 space-y-5" onSubmit={submit}>
            {mode === 'signup' && (
              <label className="field-label">
                Full name
                <input autoComplete="name" className="field" minLength={2} onChange={(event) => setFullName(event.target.value)} placeholder="Lucas Silva" required value={fullName} />
              </label>
            )}
            <label className="field-label">
              Email
              <input autoComplete="email" autoFocus className="field" onChange={(event) => setEmail(event.target.value)} placeholder="you@example.com" required type="email" value={email} />
            </label>

            {error && <p className="rounded-lg border border-rose-500/30 bg-rose-500/10 p-3 text-sm text-rose-300" role="alert">{error}</p>}

            <button className="button-primary w-full" disabled={submitting} type="submit">
              {submitting ? 'Please wait…' : mode === 'signin' ? 'Continue with email' : 'Create account'}
              {!submitting && <ArrowRight size={16} />}
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-slate-500">
            {mode === 'signin' ? 'New to StockHub?' : 'Already have an account?'}{' '}
            <button className="font-medium text-lime hover:text-lime/80" onClick={changeMode} type="button">
              {mode === 'signin' ? 'Create account' : 'Sign in'}
            </button>
          </p>
        </div>
      </section>
    </main>
  );
}
