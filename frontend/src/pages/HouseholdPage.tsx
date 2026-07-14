import { useEffect, useState } from 'react'
import { api, gbp } from '../api'
import type { HouseholdInfo, HouseholdSummary, ShareVisibility } from '../types'

const VISIBILITY_OPTIONS: { value: ShareVisibility; label: string }[] = [
  { value: 'NetWorthOnly', label: 'Net worth only' },
  { value: 'CategoryTotals', label: 'Category totals' },
  { value: 'FullBreakdown', label: 'Full breakdown' },
]

const visibilityLabel = (v: ShareVisibility) => VISIBILITY_OPTIONS.find((x) => x.value === v)?.label ?? v

export default function HouseholdPage() {
  const [household, setHousehold] = useState<HouseholdInfo | null>(null)
  const [summary, setSummary] = useState<HouseholdSummary | null>(null)
  const [inviteEmail, setInviteEmail] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const load = () => {
    api.get<HouseholdInfo>('/api/household').then((h) => {
      setHousehold(h)
      if (h.inHousehold && h.myStatus === 'Active') {
        api.get<HouseholdSummary>('/api/household/summary').then(setSummary).catch((e) => setError(e.message))
      } else {
        setSummary(null)
      }
    }).catch((e) => setError(e.message))
  }
  useEffect(load, [])

  async function invite(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setNotice(null)
    try {
      await api.post('/api/household/invite', { email: inviteEmail })
      setNotice(`Invited ${inviteEmail}.`)
      setInviteEmail('')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send invite.')
    }
  }

  async function accept() {
    setError(null)
    try {
      await api.post('/api/household/accept')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to accept invite.')
    }
  }

  async function declineOrLeave() {
    setError(null)
    try {
      await api.del('/api/household/membership')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed.')
    }
  }

  async function changeVisibility(visibility: ShareVisibility) {
    setError(null)
    try {
      await api.put('/api/household/visibility', { visibility })
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update visibility.')
    }
  }

  if (!household) return <p className="muted">Loading…</p>

  const iAmPending = household.inHousehold && household.myStatus === 'Invited'
  const iAmActive = household.inHousehold && household.myStatus === 'Active'

  return (
    <>
      <h1>Household</h1>
      <p className="lede">
        Link up with people you trust to see a combined view of your wealth — each of you
        still owns and edits only your own data. Invites only work for people who already
        have an OpenWealth account; there's no way to create a profile on someone else's
        behalf. Each member picks what they disclose to the household, independent of any
        public sharing settings.
      </p>
      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}

      {iAmPending && (
        <div className="card">
          <h2>You've been invited</h2>
          <p className="muted">Join this household to see (and share) a combined view of your wealth.</p>
          <div className="row-actions">
            <button onClick={accept}>Accept</button>
            <button className="danger" onClick={declineOrLeave}>Decline</button>
          </div>
        </div>
      )}

      {household.inHousehold && household.members && (
        <div className="card">
          <h2>Members</h2>
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Shares</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {household.members.map((m) => (
                <tr key={m.membershipId}>
                  <td>{m.displayName}{m.isMe ? ' (you)' : ''}</td>
                  <td>
                    <span className={`badge ${m.status === 'Invited' ? 'warn' : ''}`}>
                      {m.status === 'Invited' ? 'Invite pending' : 'Active'}
                    </span>
                  </td>
                  <td>
                    {m.isMe && iAmActive ? (
                      <select value={m.visibility}
                        onChange={(e) => changeVisibility(e.target.value as ShareVisibility)}>
                        {VISIBILITY_OPTIONS.map((o) => (
                          <option key={o.value} value={o.value}>{o.label}</option>
                        ))}
                      </select>
                    ) : (
                      visibilityLabel(m.visibility)
                    )}
                  </td>
                  <td className="num">
                    {m.isMe && iAmActive && (
                      <button className="danger" onClick={declineOrLeave}>Leave</button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {summary && (
        <div className="card">
          <h2>Combined summary</h2>
          <p className="muted">
            Total household net worth: <strong>{gbp.format(summary.totalNetWorth)}</strong>
          </p>
          <table>
            <thead>
              <tr>
                <th>Member</th>
                <th className="num">Net worth</th>
                <th className="num">Assets</th>
                <th className="num">Liabilities</th>
              </tr>
            </thead>
            <tbody>
              {summary.members.map((m, i) => (
                <tr key={i}>
                  <td>{m.displayName}</td>
                  <td className="num">{gbp.format(m.netWorth)}</td>
                  <td className="num">{m.totalAssets != null ? gbp.format(m.totalAssets) : '—'}</td>
                  <td className="num">{m.totalLiabilities != null ? gbp.format(m.totalLiabilities) : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {!iAmPending && (
        <div className="card">
          <h2>{household.inHousehold ? 'Invite another member' : 'Start a household'}</h2>
          <form className="grid" onSubmit={invite}>
            <div className="field">
              <label>Email of an existing OpenWealth user</label>
              <input type="email" value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)} required />
            </div>
            <button type="submit">Send invite</button>
          </form>
        </div>
      )}
    </>
  )
}
