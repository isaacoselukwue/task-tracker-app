import React, { createContext, useContext, useEffect, useRef, useState } from 'react';
import { jwtDecode } from 'jwt-decode';
import { apiLogout } from '../services/AuthService';

interface UserClaims {
  [key: string]: any;
}

export interface SessionContextProps {
  isAuthenticated: boolean;
  user: UserClaims | null;
  logout: () => void;
  refreshToken: () => Promise<void>;
  tokenExpiresIn: number | null;
  updateSession: () => void;
  bootstrapping: boolean;
}

const SessionContext = createContext<SessionContextProps>({
  isAuthenticated: false,
  user: null,
  logout: () => {},
  refreshToken: async () => {},
  tokenExpiresIn: null,
  updateSession: () => {},
  bootstrapping: true,
});

export const useSession = () => useContext(SessionContext);

export const SessionProvider = ({ children }: { children: React.ReactNode }) => {
  const refreshTimeout = useRef<NodeJS.Timeout | null>(null);
  const [user, setUser] = useState<UserClaims | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [tokenExpiresIn, setTokenExpiresIn] = useState<number | null>(null);
  const [bootstrapping, setBootstrapping] = useState(true);


  const refreshToken = async () => {
    const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
    const API_KEY = import.meta.env.VITE_BASE_API_KEY;
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) {
      handleLogout();
      return;
    }

    const response = await fetch(`${API_BASE_URL}/authentication/login/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Api-Key': API_KEY,
      },
      body: JSON.stringify({ EncryptedToken: refreshToken }),
    });

    if (response.ok) {
      const data = await response.json();
      localStorage.setItem('accessToken', data.data.accessToken.accessToken);
      localStorage.setItem('refreshToken', data.data.accessToken.refreshToken);
      localStorage.setItem('expiresIn', data.data.accessToken.expiresIn.toString());
      window.dispatchEvent(new Event('authChanged'));
      updateSession();
      scheduleRefresh(data.data.accessToken.expiresIn);
    } else {
      handleLogout();
    }
  };

  const scheduleRefresh = (expiresIn: number) => {
    if (refreshTimeout.current) clearTimeout(refreshTimeout.current);
    const timeout = (expiresIn - 60) * 1000;
    refreshTimeout.current = setTimeout(refreshToken, timeout > 0 ? timeout : 0);
    setTokenExpiresIn(expiresIn);
  };

    const updateSession = () => {
    const accessToken = localStorage.getItem('accessToken');
    if (accessToken) {
        try {
        const decoded = jwtDecode<UserClaims>(accessToken);
        setUser(decoded);
        setIsAuthenticated(true);
        if (decoded.exp) {
            const now = Math.floor(Date.now() / 1000);
            setTokenExpiresIn(decoded.exp - now);
        }
        } catch {
        setUser(null);
        setIsAuthenticated(false);
        }
    } else {
        setUser(null);
        setIsAuthenticated(false);
    }
    setBootstrapping(false);
    };

    const handleLogout = async () => {
        try {
            await apiLogout();
        } catch (e) {
        }
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('expiresIn');
        setUser(null);
        setIsAuthenticated(false);
        window.location.href = '/login';
    };

    useEffect(() => {
    updateSession();
    setBootstrapping(false);
    return () => {
        if (refreshTimeout.current) clearTimeout(refreshTimeout.current);
    };
    }, []);

    useEffect(() => {
  if (isAuthenticated && tokenExpiresIn) {
    scheduleRefresh(tokenExpiresIn);
  }
  return () => {
    if (refreshTimeout.current) clearTimeout(refreshTimeout.current);
  };
}, [isAuthenticated, tokenExpiresIn]);

  useEffect(() => {
    const handler = () => {
      updateSession();
    };
    window.addEventListener('authChanged', handler);
    return () => window.removeEventListener('authChanged', handler);
  }, []);

  return (
    <SessionContext.Provider value={{
      isAuthenticated,
      user,
      logout: handleLogout,
      refreshToken,
      tokenExpiresIn,
      updateSession,
      bootstrapping
    }}>
      {children}
    </SessionContext.Provider>
  );
};