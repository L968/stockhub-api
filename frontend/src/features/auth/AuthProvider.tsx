import { useMemo, useState, type ReactNode } from 'react';
import { api } from '../../lib/api';
import { queryClient } from '../../lib/query-client';
import { clearUser, loadUser, saveUser } from '../../lib/storage';
import type { User } from '../../types/api';
import { AuthContext } from './AuthContext';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(() => loadUser());

  const value = useMemo(() => ({
    user,
    async signIn(email: string) {
      const normalizedEmail = email.trim();
      const { userId } = await api.auth.signIn(normalizedEmail);
      const fallback = { userId, email: normalizedEmail, fullName: normalizedEmail.split('@')[0] };
      saveUser(fallback);
      const currentUser = await api.auth.me().catch(() => fallback);
      saveUser(currentUser);
      setUser(currentUser);
    },
    async signUp(email: string, fullName: string) {
      const normalizedEmail = email.trim();
      const normalizedName = fullName.trim();
      const { userId } = await api.auth.signUp(normalizedEmail, normalizedName);
      const newUser = { userId, email: normalizedEmail, fullName: normalizedName };
      saveUser(newUser);
      setUser(newUser);
    },
    signOut() {
      clearUser();
      queryClient.clear();
      setUser(null);
    },
  }), [user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
