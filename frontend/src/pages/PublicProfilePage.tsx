import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { api, gbp } from '../api'
import { TrendChart, TREND_HORIZONS, WEALTH_SERIES } from '../components/TrendChart'
import type { PublicProfile } from '../types'

const CATEGORY_TREND_SERIES = [
  { key: 'netWorth', label: 'Net worth', color: '#3987e5' },
  { key: 'totalAssets', label: 'Assets', color: '#199e70' },
  { key: 'totalLiabilities', label: 'Liabilities', color: '#e66767' },
] as const

const NET_WORTH_TREND_SERIES = [
  { key: 'netWorth', label: 'Net worth', color: '#3987e5' },
] as const

function seriesForVisibility(visibility: PublicProfile['visibility']) {
  if (visibility === 'FullBreakdown') return WEALTH_SERIES
  if (visibility === 'CategoryTotals') return CATEGORY_TREND_SERIES
  return NET_WORTH_TREND_SERIES
}

export default function PublicProfilePage() {
  const { slug } = useParams<{ slug: string }>()
  const [passphrase, setPassphrase] = useState('')
  const [profile, setProfile] = useState<PublicProfile | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [months, setMonths] = useState(60)

  async function unlock(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      setProfile(await api.post<PublicProfile>(`/api/public/${slug}`, { passphrase }))
    } catch (err) {
      const status = (err as { status?: number }).status
      setError(
        status === 401
          ? 'Wrong passphrase.'
          : status === 404
            ? 'This profile does not exist or sharing is turned off.'
            : 'Something went wrong.',
      )
    } finally {
      setBusy(false)
    }
  }

  if (!profile) {
    return (
      <div className="auth-wrap">
        <div className="auth-card">
          <h1>
            <span className="brand-mark">£</span> OpenWealth
          </h1>
          <p className="lede" style={{ textAlign: 'center' }}>
            This profile is protected. Enter the passphrase you were given.
          </p>
          {error && <div className="error-box">{error}</div>}
          <form onSubmit={unlock}>
            <div className="field">
              <label htmlFor="pass">Passphrase</label>
              <input id="pass" type="password" value={passphrase}
                onChange={(e) => setPassphrase(e.target.value)} required autoFocus />
            </div>
            <button type="submit" disabled={busy}>View profile</button>
          </form>
        </div>
      </div>
    )
  }

  return (
    <div className="public-wrap">
      <h1>{profile.displayName}'s wealth profile</h1>
      <p className="lede">Shared via OpenWealth.</p>

      <div className="stat-grid">
        <div className="stat">
          <div className="label">Net worth</div>
          <div className={`value ${profile.netWorth >= 0 ? 'positive' : 'negative'}`}>
            {gbp.format(profile.netWorth)}
          </div>
        </div>
        {profile.totalAssets !== undefined && (
          <div className="stat">
            <div className="label">Assets</div>
            <div className="value">{gbp.format(profile.totalAssets)}</div>
          </div>
        )}
        {profile.totalLiabilities !== undefined && (
          <div className="stat">
            <div className="label">Liabilities</div>
            <div className="value">{gbp.format(profile.totalLiabilities)}</div>
          </div>
        )}
      </div>

      {profile.history.length + profile.projection.length > 1 && (
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
            <h2 style={{ margin: 0 }}>Net worth over time</h2>
            <div style={{ display: 'flex', gap: '0.35rem' }}>
              {TREND_HORIZONS.map((h) => (
                <button
                  key={h.months}
                  type="button"
                  className="secondary"
                  style={months === h.months
                    ? { borderColor: 'var(--accent)', color: 'var(--accent)' }
                    : undefined}
                  onClick={() => setMonths(h.months)}
                >
                  {h.label}
                </button>
              ))}
            </div>
          </div>
          <TrendChart
            historic={profile.history}
            projected={profile.projection.slice(0, months + 1)}
            series={seriesForVisibility(profile.visibility)}
            emptyMessage="Not enough recorded history yet."
          />
          <p className="muted" style={{ margin: 0 }}>
            Solid lines are recorded history; dashed lines are projected from current balances,
            rates and repayments.
          </p>
        </div>
      )}

      {(profile.assetTotals || profile.liabilityTotals) && !profile.items && (
        <div className="card">
          <h2>By category</h2>
          <table>
            <tbody>
              {[...(profile.assetTotals ?? []), ...(profile.liabilityTotals ?? [])].map((c) => (
                <tr key={c.category}>
                  <td>{c.category}</td>
                  <td className="num">{gbp.format(c.total)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {profile.items && (
        <div className="card">
          <h2>Full breakdown</h2>
          <table>
            <thead>
              <tr>
                <th>Category</th>
                <th>Item</th>
                <th className="num">Value</th>
              </tr>
            </thead>
            <tbody>
              {profile.items.map((item, i) => (
                <tr key={i}>
                  <td>{item.category}</td>
                  <td>{item.name}</td>
                  <td className="num">{gbp.format(item.value)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
