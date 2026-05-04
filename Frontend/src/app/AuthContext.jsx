import { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import PropTypes from 'prop-types';
import { login as apiLogin, logout as apiLogout, register as apiRegister } from '../api/auth';
import { tokenStore } from '../api/client';
import apiClient from '../api/client';

/** @typedef {{ userId: string, name: string, email: string, role: string, isActive: boolean }} CurrentUser */

/**
 * Decode a JWT payload without verifying the signature.
 * @param {string} token
 * @returns {Record<string, any>|null}
 */
function decodeJwtPayload(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(base64));
  } catch {
    return null;
  }
}

/** Check if a JWT is expired */
function isTokenExpired(token) {
  const payload = decodeJwtPayload(token);
  if (!payload?.exp) return false;
  return Date.now() / 1000 > payload.exp;
}

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true); // true while validating session on startup
  const validated = useRef(false);

  // On mount: restore session and validate role against DB
  useEffect(() => {
    if (validated.current) return;
    validated.current = true;

    const storedUser = sessionStorage.getItem('ip_user');
    const storedToken = sessionStorage.getItem('ip_token');

    if (!storedUser || !storedToken) { setLoading(false); return; }

    // If token is already expired, clear immediately
    if (isTokenExpired(storedToken)) {
      sessionStorage.removeItem('ip_user');
      sessionStorage.removeItem('ip_token');
      setLoading(false);
      return;
    }

    // Restore token so the API call below can authenticate
    tokenStore.set(storedToken);

    // Hit /auth/me to get the authoritative role from the DB
    apiClient.get('/identity/auth/me')
      .then(({ data }) => {
        // data is UserResponse: { userId, name, email, role, isActive, createdAt }
        const freshUser = {
          userId: data.userId,
          name: data.name,
          email: data.email,
          role: data.role,
          isActive: data.isActive,
        };
        setUser(freshUser);
        sessionStorage.setItem('ip_user', JSON.stringify(freshUser));
      })
      .catch(() => {
        // Token invalid or user deleted — clear session
        tokenStore.clear();
        sessionStorage.removeItem('ip_user');
        sessionStorage.removeItem('ip_token');
      })
      .finally(() => setLoading(false));
  }, []);

  // Forced logout (401 from interceptor)
  useEffect(() => {
    const handler = () => {
      setUser(null);
      sessionStorage.removeItem('ip_user');
      sessionStorage.removeItem('ip_token');
    };
    window.addEventListener('auth:logout', handler);
    return () => window.removeEventListener('auth:logout', handler);
  }, []);

  /** @param {{ email: string, password: string }} credentials */
  const login = useCallback(async (credentials) => {
    // Do NOT set loading=true here — that remounts the router via AppRoutes
    // and wipes the error state in LoginPage on failure.
    try {
      const data = await apiLogin(credentials);
      const freshUser = {
        userId: data.user.userId,
        name: data.user.name,
        email: data.user.email,
        role: data.user.role,
        isActive: data.user.isActive,
      };
      tokenStore.set(data.accessToken);
      setUser(freshUser);
      sessionStorage.setItem('ip_user', JSON.stringify(freshUser));
      sessionStorage.setItem('ip_token', data.accessToken);
      return freshUser;
    } catch (err) {
      // Re-throw so LoginPage can catch it and display the error message
      throw err;
    }
  }, []);

  /** @param {{ name: string, email: string, password: string }} data */
  const register = useCallback(async (data) => {
    try {
      const res = await apiRegister(data);
      const freshUser = {
        userId: res.user.userId,
        name: res.user.name,
        email: res.user.email,
        role: res.user.role,
        isActive: res.user.isActive,
      };
      tokenStore.set(res.accessToken);
      setUser(freshUser);
      sessionStorage.setItem('ip_user', JSON.stringify(freshUser));
      sessionStorage.setItem('ip_token', res.accessToken);
      return freshUser;
    } catch (err) {
      throw err;
    }
  }, []);

  const logout = useCallback(async () => {
    await apiLogout();
    tokenStore.clear();
    setUser(null);
    sessionStorage.removeItem('ip_user');
    sessionStorage.removeItem('ip_token');
  }, []);

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, register }}>
      {children}
    </AuthContext.Provider>
  );
}

AuthProvider.propTypes = {
  children: PropTypes.node.isRequired,
};

/** @returns {{ user: CurrentUser|null, loading: boolean, login: Function, logout: Function, register: Function }} */
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
