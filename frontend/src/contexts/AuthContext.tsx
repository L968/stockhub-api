import { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { authService } from '../services/api';
import { setUserId, removeUserId, setCurrentUser, removeCurrentUser, getCurrentUser } from '../lib/auth';

interface User {
  id: string;
  email: string;
  fullName: string;
}

interface AuthContextType {
  user: User | null;
  loading: boolean;
  signUp: (email: string, fullName: string) => Promise<void>;
  signIn: (email: string) => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const savedUser = getCurrentUser();
    if (savedUser) {
      setUser(savedUser);
    }
    setLoading(false);
  }, []);

  const signUp = async (email: string, fullName: string) => {
    const response = await authService.signUp(email, fullName);
    const userId = response.userId;

    if (!userId) {
      throw new Error('Failed to get user ID from response');
    }

    const user = {
      id: userId,
      email,
      fullName,
    };

    setUserId(userId);
    setCurrentUser(user);
    setUser(user);
  };

  const signIn = async (email: string) => {
    const response = await authService.signIn(email);
    const userId = response.userId;

    if (!userId) {
      throw new Error('Failed to get user ID from response');
    }

    setUserId(userId);

    try {
      const userData = await authService.getMe();
      const user = {
        id: userId,
        email: userData.email || email,
        fullName: userData.fullName || email.split('@')[0],
      };
      setCurrentUser(user);
      setUser(user);
    } catch {
      const user = {
        id: userId,
        email,
        fullName: email.split('@')[0],
      };
      setCurrentUser(user);
      setUser(user);
    }
  };

  const signOut = async () => {
    removeUserId();
    removeCurrentUser();
    setUser(null);
  };

  const value: AuthContextType = {
    user,
    loading,
    signUp,
    signIn,
    signOut,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
