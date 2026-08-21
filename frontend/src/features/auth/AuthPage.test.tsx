import { render, screen } from '@testing-library/react';
import { AuthContext } from './AuthContext';
import { AuthPage } from './AuthPage';

describe('AuthPage', () => {
  it('prefills the seeded demo user email', () => {
    render(
      <AuthContext.Provider value={{
        user: null,
        signIn: vi.fn(),
        signUp: vi.fn(),
        signOut: vi.fn(),
      }}>
        <AuthPage />
      </AuthContext.Provider>,
    );

    expect(screen.getByRole('textbox', { name: 'Email' })).toHaveValue('demo@stockhub.dev');
  });
});
