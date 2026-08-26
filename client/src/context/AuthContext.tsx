import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, setAuthToken } from '../api/client';
import type { User } from '../types';

interface AuthContextValue {
  user: User | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, fullName: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);
const TOKEN_KEY = 'lv_auth_token';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_KEY);
    setAuthToken(null);
    setUser(null);
  }, []);

  useEffect(() => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) {
      setIsLoading(false);
      return;
    }

    setAuthToken(token);
    api.auth.me()
      .then(setUser)
      .catch(() => logout())
      .finally(() => setIsLoading(false));
  }, [logout]);

  const login = useCallback(async (email: string, password: string) => {
    const result = await api.auth.login({ email, password });
    localStorage.setItem(TOKEN_KEY, result.token);
    setAuthToken(result.token);
    setUser(result.user);
  }, []);

  const register = useCallback(async (email: string, password: string, fullName: string) => {
    const result = await api.auth.register({ email, password, fullName });
    localStorage.setItem(TOKEN_KEY, result.token);
    setAuthToken(result.token);
    setUser(result.user);
  }, []);

  const value = useMemo(
    () => ({ user, isLoading, login, register, logout }),
    [user, isLoading, login, register, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
