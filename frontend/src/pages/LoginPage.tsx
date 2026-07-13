import { useState } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth'

export default function LoginPage() {
  const { isAuthenticated, login, register } = useAuth()
  const location = useLocation()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (isAuthenticated) {
    const from = (location.state as { from?: { pathname: string } })?.from?.pathname ?? '/'
    return <Navigate to={from} replace />
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      if (mode === 'login') await login(email, password)
      else await register(email, password, displayName)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-wrap">
      <div className="auth-card">
        <h1>
          <span className="brand-mark">£</span> OpenWealth
        </h1>
        <p className="lede" style={{ textAlign: 'center' }}>
          {mode === 'login' ? 'Welcome back.' : 'Create your account. Your data stays yours.'}
        </p>
        {error && <div className="error-box">{error}</div>}
        <form onSubmit={submit}>
          {mode === 'register' && (
            <div className="field">
              <label htmlFor="name">Display name</label>
              <input id="name" value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
            </div>
          )}
          <div className="field">
            <label htmlFor="email">Email</label>
            <input id="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div className="field">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              minLength={mode === 'register' ? 10 : undefined}
              required
            />
            {mode === 'register' && <span className="muted">At least 10 characters.</span>}
          </div>
          <button type="submit" disabled={busy}>
            {mode === 'login' ? 'Log in' : 'Create account'}
          </button>
        </form>
        <div className="auth-toggle">
          {mode === 'login' ? (
            <>
              New here?{' '}
              <button className="link-button" onClick={() => setMode('register')}>Create an account</button>
            </>
          ) : (
            <>
              Already registered?{' '}
              <button className="link-button" onClick={() => setMode('login')}>Log in</button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
