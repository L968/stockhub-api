import type { User } from '../types/api';

const USER_KEY = 'stockhub.user';

export function loadUser(): User | null {
  const value = localStorage.getItem(USER_KEY);
  if (!value) return null;
  try {
    const user = JSON.parse(value) as Partial<User>;
    return user.userId && user.email && user.fullName ? (user as User) : null;
  } catch {
    localStorage.removeItem(USER_KEY);
    return null;
  }
}

export const saveUser = (user: User): void => localStorage.setItem(USER_KEY, JSON.stringify(user));
export const clearUser = (): void => localStorage.removeItem(USER_KEY);
export const getUserId = (): string | null => loadUser()?.userId ?? null;
