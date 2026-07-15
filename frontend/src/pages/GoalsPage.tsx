import { useEffect, useState } from 'react'
import { api, gbp } from '../api'
import { useInlineEdit } from '../hooks/useInlineEdit'
import type { Goal, GoalMetric } from '../types'

const METRICS: { value: GoalMetric; label: string }[] = [
  { value: 'NetWorth', label: 'Net worth' },
  { value: 'Savings', label: 'Savings' },
  { value: 'Investments', label: 'Investments' },
  { value: 'TotalAssets', label: 'Total assets' },
  { value: 'TotalLiabilities', label: 'Total debts (lower is better)' },
]

const metricLabel = (m: GoalMetric) => METRICS.find((x) => x.value === m)?.label ?? m

function goalRequest(g: Goal) {
  return { name: g.name, metric: g.metric, targetAmount: g.targetAmount, targetDate: g.targetDate }
}

export default function GoalsPage() {
  const [goals, setGoals] = useState<Goal[]>([])
  const [name, setName] = useState('')
  const [metric, setMetric] = useState<GoalMetric>('NetWorth')
  const [targetAmount, setTargetAmount] = useState('')
  const [targetDate, setTargetDate] = useState('')
  const [error, setError] = useState<string | null>(null)
  const edit = useInlineEdit<Goal>()

  const load = () => {
    api.get<Goal[]>('/api/goals').then(setGoals).catch((e) => setError(e.message))
  }
  useEffect(load, [])

  async function add(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/goals', {
        name,
        metric,
        targetAmount: Number(targetAmount),
        targetDate,
      })
      setName(''); setMetric('NetWorth'); setTargetAmount(''); setTargetDate('')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add goal.')
    }
  }

  async function saveEdit() {
    if (!edit.draft) return
    setError(null)
    try {
      await api.put(`/api/goals/${edit.editingId}`, goalRequest(edit.draft))
      edit.cancelEdit()
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save changes.')
    }
  }

  async function remove(id: string) {
    await api.del(`/api/goals/${id}`)
    load()
  }

  return (
    <>
      <h1>Goals</h1>
      <p className="lede">
        Set a target for one of your wealth figures by a date, and see — using the exact same
        maths as the Trends projection — whether your current balances, rates and repayments
        are actually on track to get you there.
      </p>
      {error && <div className="error-box">{error}</div>}

      <div className="card">
        {goals.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Tracking</th>
                <th className="num">Target</th>
                <th>By</th>
                <th className="num">Current</th>
                <th className="num">Projected</th>
                <th>Status</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {goals.map((g) =>
                edit.isEditing(g.id) && edit.draft ? (
                  <tr key={g.id} className="editing-row">
                    <td>
                      <input value={edit.draft.name}
                        onChange={(e) => edit.updateDraft({ name: e.target.value })} />
                    </td>
                    <td>
                      <select value={edit.draft.metric}
                        onChange={(e) => edit.updateDraft({ metric: e.target.value as GoalMetric })}>
                        {METRICS.map((m) => (
                          <option key={m.value} value={m.value}>{m.label}</option>
                        ))}
                      </select>
                    </td>
                    <td className="num">
                      <input type="number" step="0.01" value={edit.draft.targetAmount}
                        onChange={(e) => edit.updateDraft({ targetAmount: Number(e.target.value) })} />
                    </td>
                    <td>
                      <input type="date" value={edit.draft.targetDate}
                        onChange={(e) => edit.updateDraft({ targetDate: e.target.value })} />
                    </td>
                    <td className="num">{gbp.format(g.currentValue)}</td>
                    <td className="num">{gbp.format(g.projectedValue)}</td>
                    <td>—</td>
                    <td className="num">
                      <div className="row-actions">
                        <button onClick={saveEdit}>Save</button>
                        <button className="secondary" onClick={edit.cancelEdit}>Cancel</button>
                      </div>
                    </td>
                  </tr>
                ) : (
                  <tr key={g.id}>
                    <td>{g.name}</td>
                    <td>{metricLabel(g.metric)}</td>
                    <td className="num">{gbp.format(g.targetAmount)}</td>
                    <td>{g.targetDate}</td>
                    <td className="num">{gbp.format(g.currentValue)}</td>
                    <td className="num">{gbp.format(g.projectedValue)}</td>
                    <td>
                      <span className={`badge ${g.onTrack ? '' : 'warn'}`}>
                        {g.onTrack ? 'On track' : 'Behind'}
                      </span>
                    </td>
                    <td className="num">
                      <div className="row-actions">
                        <button className="secondary" onClick={() => edit.startEdit(g)}>Edit</button>
                        <button className="danger" onClick={() => remove(g.id)}>Remove</button>
                      </div>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        )}
        <form className="grid" onSubmit={add} style={{ marginTop: goals.length ? '1rem' : 0 }}>
          <div className="field">
            <label>Name</label>
            <input value={name} onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Mortgage-free" required />
          </div>
          <div className="field">
            <label>Tracking</label>
            <select value={metric} onChange={(e) => setMetric(e.target.value as GoalMetric)}>
              {METRICS.map((m) => (
                <option key={m.value} value={m.value}>{m.label}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Target amount (£)</label>
            <input type="number" step="0.01" value={targetAmount}
              onChange={(e) => setTargetAmount(e.target.value)} required />
            <span className="muted">For a debt target, this is usually £0.</span>
          </div>
          <div className="field">
            <label>By</label>
            <input type="date" value={targetDate}
              onChange={(e) => setTargetDate(e.target.value)} required />
          </div>
          <button type="submit">Add goal</button>
        </form>
      </div>
    </>
  )
}
