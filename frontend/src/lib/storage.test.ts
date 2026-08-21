import { clearUser, getUserId, loadUser, saveUser } from './storage';

describe('user storage', () => {
  it('stores and restores a valid user', () => {
    const user = { userId: 'user-1', email: 'trader@stockhub.dev', fullName: 'Trader' };
    saveUser(user);
    expect(loadUser()).toEqual(user);
    expect(getUserId()).toBe('user-1');
  });

  it('discards malformed persisted data', () => {
    localStorage.setItem('stockhub.user', '{broken');
    expect(loadUser()).toBeNull();
    expect(localStorage.getItem('stockhub.user')).toBeNull();
  });

  it('clears the current user', () => {
    saveUser({ userId: 'user-1', email: 'trader@stockhub.dev', fullName: 'Trader' });
    clearUser();
    expect(loadUser()).toBeNull();
  });
});
