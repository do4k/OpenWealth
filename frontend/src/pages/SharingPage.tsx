import { useEffect, useState } from 'react'
import { api } from '../api'
import type { ShareSettings, ShareVisibility } from '../types'

export default function SharingPage() {
  const [settings, setSettings] = useState<ShareSettings | null>(null)
  const [passphrase, setPassphrase] = useState('')
  const [visibility, setVisibility] = useState<ShareVisibility>('NetWorthOnly')
  const [enabled, setEnabled] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    api.get<ShareSettings>('/api/share').then((s) => {
      setSettings(s)
      setEnabled(s.enabled)
      setVisibility(s.visibility)
    }).catch((e) => setError(e.message))
  }, [])

  async function save(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSaved(false)
    try {
      const updated = await api.put<ShareSettings>('/api/share', {
        enabled,
        passphrase: passphrase || null,
        visibility,
      })
      setSettings(updated)
      setPassphrase('')
      setSaved(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    }
  }

  async function rotate() {
    try {
      const res = await api.post<{ slug: string }>('/api/share/rotate-link')
      setSettings((s) => (s ? { ...s, slug: res.slug } : s))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to rotate link.')
    }
  }

  const shareUrl = settings?.slug ? `${window.location.origin}/p/${settings.slug}` : null

  async function copy() {
    if (!shareUrl) return
    await navigator.clipboard.writeText(shareUrl)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  if (!settings) return <p className="muted">Loading…</p>

  return (
    <>
      <h1>Sharing</h1>
      <p className="lede">
        Share a read-only view of your wealth with people you trust — protected by a
        passphrase you give them. Your data stays yours: sharing is opt-in and only ever
        covers your own profile.
      </p>
      {error && <div className="error-box">{error}</div>}
      {saved && <div className="success-box">Sharing settings saved.</div>}

      <div className="card">
        <form className="grid" onSubmit={save}>
          <div className="field checkbox">
            <input id="share-enabled" type="checkbox" checked={enabled}
              onChange={(e) => setEnabled(e.target.checked)} />
            <label htmlFor="share-enabled">Enable my public profile</label>
          </div>
          <div className="field">
            <label>What viewers can see</label>
            <select value={visibility} onChange={(e) => setVisibility(e.target.value as ShareVisibility)}>
              <option value="NetWorthOnly">Net worth only</option>
              <option value="CategoryTotals">Category totals</option>
              <option value="FullBreakdown">Full breakdown</option>
            </select>
          </div>
          <div className="field">
            <label>
              {settings.hasPassphrase ? 'Change passphrase (leave blank to keep)' : 'Set a passphrase'}
            </label>
            <input type="password" value={passphrase} minLength={6}
              onChange={(e) => setPassphrase(e.target.value)}
              placeholder={settings.hasPassphrase ? '••••••••' : 'At least 6 characters'} />
          </div>
          <button type="submit">Save</button>
        </form>
      </div>

      {settings.enabled && shareUrl && (
        <div className="card">
          <h2>Your share link</h2>
          <p>
            <a href={shareUrl}>{shareUrl}</a>
          </p>
          <p className="muted">
            Anyone with this link and your passphrase can view your profile. Rotating the
            link invalidates the old URL.
          </p>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button className="secondary" onClick={copy}>{copied ? 'Copied!' : 'Copy link'}</button>
            <button className="secondary" onClick={rotate}>Rotate link</button>
          </div>
        </div>
      )}
    </>
  )
}
