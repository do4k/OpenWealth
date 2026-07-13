import { useEffect, useState } from 'react'
import { api, gbp } from '../api'
import type { HouseholdSummary, HouseholdView } from '../types'

export default function HouseholdPage() {
  const [view, setView] = useState<HouseholdView | null>(null)
  const [summary, setSummary] = useState<HouseholdSummary | null>(null)
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const load = () => {
    api.get<HouseholdView>('/api/household').then(setView).catch((e) => setError(e.message))
    api.get<HouseholdSummary>('/api/household/summary').then(setSummary).catch((e) => setError(e.message))
  }
  useEffect(load, [])

  async function invite(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setNotice(null)
    try {
      setView(await api.post<HouseholdView>('/api/household/invite', { email }))
      setNotice(`Invite sent to ${email}. They'll see it on their Household page.`)
      setEmail('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to invite.')
    }
  }

  async function respond(linkId: string, accept: boolean) {
    setError(null)
    setNotice(null)
    try {
      setView(await api.post<HouseholdView>('/api/household/respond', { linkId, accept }))
      setNotice(accept ? 'You are now linked.' : 'Invite declined.')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to respond.')
    }
  }

  async function unlink(linkId: string) {
    setError(null)
    setNotice(null)
    await api.del(`/api/household/${linkId}`)
    setNotice('Unlinked. They can no longer see your totals, and you can no longer see theirs.')
    load()
  }

  const hasPartners = (view?.members.length ?? 0) > 0

  return (
    <>
      <h1>Household</h1>
      <p className="lede">
        Link with the people you care about to see a combined picture. Everyone owns and
        edits only their own data — a link shares net worth and category totals, both sides
        must consent, and either side can unlink at any time.
      </p>
      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}

      {summary && hasPartners && (
        <>
          <div className="stat-grid">
            <div className="stat">
              <div className="label">Household net worth</div>
              <div className={`value ${summary.combinedNetWorth >= 0 ? 'positive' : 'negative'}`}>
                {gbp.format(summary.combinedNetWorth)}
              </div>
            </div>
            <div className="stat">
              <div className="label">Combined assets</div>
              <div className="value">{gbp.format(summary.combinedAssets)}</div>
            </div>
            <div className="stat">
              <div className="label">Combined liabilities</div>
              <div className="value">{gbp.format(summary.combinedLiabilities)}</div>
            </div>
          </div>

          <div className="card">
            <h2>Members</h2>
            <table>
              <thead>
                <tr>
                  <th>Member</th>
                  <th className="num">Assets</th>
                  <th className="num">Liabilities</th>
                  <th className="num">Net worth</th>
                </tr>
              </thead>
              <tbody>
                {summary.members.map((m, i) => (
                  <tr key={i}>
                    <td>
                      {m.displayName} {m.isYou && <span className="badge">You</span>}
                    </td>
                    <td className="num">{gbp.format(m.totalAssets)}</td>
                    <td className="num">{gbp.format(m.totalLiabilities)}</td>
                    <td className="num">{gbp.format(m.netWorth)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="card">
            <h2>By category</h2>
            {summary.members.map((m, i) => (
              <div key={i} style={{ marginBottom: i < summary.members.length - 1 ? '1rem' : 0 }}>
                <h3 style={{ fontSize: '0.95rem', margin: '0 0 0.4rem' }}>
                  {m.displayName} {m.isYou && <span className="badge">You</span>}
                </h3>
                <table>
                  <tbody>
                    {[...m.assetTotals, ...m.liabilityTotals].map((c) => (
                      <tr key={c.category}>
                        <td>{c.category}</td>
                        <td className="num">{gbp.format(c.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ))}
          </div>
        </>
      )}

      {view && (
        <>
          {view.invitesReceived.length > 0 && (
            <div className="card">
              <h2>Invites for you</h2>
              <table>
                <tbody>
                  {view.invitesReceived.map((inv) => (
                    <tr key={inv.linkId}>
                      <td>
                        <strong>{inv.displayName}</strong>{' '}
                        <span className="muted">({inv.email})</span> wants to link households
                      </td>
                      <td className="num">
                        <div className="row-actions">
                          <button onClick={() => respond(inv.linkId, true)}>Accept</button>
                          <button className="danger" onClick={() => respond(inv.linkId, false)}>
                            Decline
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <div className="card">
            <h2>Manage links</h2>
            {view.members.length > 0 && (
              <table>
                <tbody>
                  {view.members.map((m) => (
                    <tr key={m.linkId}>
                      <td>
                        {m.displayName} <span className="muted">({m.email})</span>
                      </td>
                      <td className="num">
                        <div className="row-actions">
                          <button className="danger" onClick={() => unlink(m.linkId)}>Unlink</button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            {view.invitesSent.length > 0 && (
              <table style={{ marginTop: view.members.length ? '0.5rem' : 0 }}>
                <tbody>
                  {view.invitesSent.map((inv) => (
                    <tr key={inv.linkId}>
                      <td>
                        {inv.displayName} <span className="muted">({inv.email})</span>{' '}
                        <span className="badge warn">Awaiting their accept</span>
                      </td>
                      <td className="num">
                        <div className="row-actions">
                          <button className="danger" onClick={() => unlink(inv.linkId)}>Withdraw</button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            <form className="grid" onSubmit={invite}
              style={{ marginTop: view.members.length + view.invitesSent.length ? '1rem' : 0 }}>
              <div className="field">
                <label>Invite by account email</label>
                <input type="email" value={email} onChange={(e) => setEmail(e.target.value)}
                  placeholder="partner@example.com" required />
              </div>
              <button type="submit">Send invite</button>
            </form>
            <p className="muted" style={{ marginBottom: 0 }}>
              They need their own OpenWealth account first — you can't create a profile on
              someone else's behalf.
            </p>
          </div>
        </>
      )}
    </>
  )
}
