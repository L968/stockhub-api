export const getUserId = (): string | null => {
  return localStorage.getItem('userId');
};

export const setUserId = (userId: string): void => {
  localStorage.setItem('userId', userId);
};

export const removeUserId = (): void => {
  localStorage.removeItem('userId');
};

export interface StoredUser {
  id: string;
  email: string;
  fullName: string;
}

export const getCurrentUser = (): StoredUser | null => {
  const userJson = localStorage.getItem('currentUser');
  return userJson ? JSON.parse(userJson) as StoredUser : null;
};

export const setCurrentUser = (user: StoredUser): void => {
  localStorage.setItem('currentUser', JSON.stringify(user));
};

export const removeCurrentUser = (): void => {
  localStorage.removeItem('currentUser');
};

