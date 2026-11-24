import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, tokenStore } from './api'
import type { AuthResponse, Role } from './types'

interface User { email: string; role: Role }

interface AuthContextValue {
  user: User | null
  signIn: (email: string, password: string) => Promise<void>
  signUp: (email: string, password: string) => Promise<void>
  signOut: () => void
}

const USER_KEY = 'rf_user'
const AuthContext = createContext<AuthContextValue | null>(null)

function loadUser(): User | null {
  try {
    const raw = localStorage.getItem(USER_KEY)
    return raw && tokenStore.get() ? (JSON.parse(raw) as User) : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(loadUser)

  const accept = useCallback((res: AuthResponse) => {
    tokenStore.set(res.token)
    const u = { email: res.email, role: res.role }
    localStorage.setItem(USER_KEY, JSON.stringify(u))
    setUser(u)
  }, [])

  const signOut = useCallback(() => {
    tokenStore.clear()
    localStorage.removeItem(USER_KEY)
    setUser(null)
  }, [])

  useEffect(() => {
    window.addEventListener('rf-unauthorized', signOut)
    return () => window.removeEventListener('rf-unauthorized', signOut)
  }, [signOut])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      signIn: async (email, password) => accept(await api.login(email, password)),
      signUp: async (email, password) => accept(await api.register(email, password)),
      signOut,
    }),
    [user, accept, signOut],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}

export const isStaff = (role: Role) => role !== 'Customer'
export const canSeeKpis = (role: Role) => role === 'Manager' || role === 'Admin'
