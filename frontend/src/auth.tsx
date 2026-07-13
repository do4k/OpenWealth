import { createContext, useCallback, useContext, useState, type ReactNode } from 'react'
import { api, getToken, setToken } from './api'
import type { AuthResponse } from './types'

interface AuthState {
  isAuthenticated: boolean
  displayName: string | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthState | null>(null)

const NAME_KEY = 'openwealth.displayName'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [authed, setAuthed] = useState(() => getToken() !== null)
  const [displayName, setDisplayName] = useState(() => localStorage.getItem(NAME_KEY))

  const accept = useCallback((res: AuthResponse) => {
    setToken(res.token)
    localStorage.setItem(NAME_KEY, res.displayName)
    setDisplayName(res.displayName)
    setAuthed(true)
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    accept(await api.post<AuthResponse>('/api/auth/login', { email, password }))
  }, [accept])

  const register = useCallback(async (email: string, password: string, name: string) => {
    accept(await api.post<AuthResponse>('/api/auth/register', { email, password, displayName: name }))
  }, [accept])

  const logout = useCallback(() => {
    setToken(null)
    localStorage.removeItem(NAME_KEY)
    setAuthed(false)
    setDisplayName(null)
  }, [])

  return (
    <AuthContext.Provider value={{ isAuthenticated: authed, displayName, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
