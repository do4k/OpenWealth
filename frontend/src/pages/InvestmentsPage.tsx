import { useEffect, useState } from 'react'
import { api, gbp } from '../api'
import { useInlineEdit } from '../hooks/useInlineEdit'
import type { Investment, InvestmentType } from '../types'

const TYPES: { value: InvestmentType; label: string }[] = [
  { value: 'StocksAndSharesIsa', label: 'Stocks & Shares ISA' },
  { value: 'GeneralInvestmentAccount', label: 'General investment account' },
  { value: 'PensionPot', label: 'Pension pot' },
  { value: 'LifetimeIsa', label: 'Lifetime ISA' },
  { value: 'Crypto', label: 'Crypto' },
  { value: 'Other', label: 'Other' },
]

const typeLabel = (t: InvestmentType) => TYPES.find((x) => x.value === t)?.label ?? t

function toRequest(i: Investment) {
  return {
    name: i.name,
    type: i.type,
    currentValue: i.currentValue,
    expectedAnnualGrowthPercent: i.expectedAnnualGrowthPercent,
    receivesIncomePensionContributions: i.type === 'PensionPot' && i.receivesIncomePensionContributions,
  }
}

export default function InvestmentsPage() {
  const [investments, setInvestments] = useState<Investment[]>([])
  const [name, setName] = useState('')
  const [type, setType] = useState<InvestmentType>('StocksAndSharesIsa')
  const [value, setValue] = useState('')
  const [growth, setGrowth] = useState('')
  const [pensionContribution, setPensionContribution] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const edit = useInlineEdit<Investment>()

  const load = () => {
    api.get<Investment[]>('/api/investments').then(setInvestments).catch((e) => setError(e.message))
  }
  useEffect(load, [])

  async function add(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/investments', {
        name,
        type,
        currentValue: Number(value),
        expectedAnnualGrowthPercent: growth ? Number(growth) : null,
        receivesIncomePensionContributions: type === 'PensionPot' && pensionContribution,
      })
      setName(''); setValue(''); setGrowth(''); setPensionContribution(false)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add investment.')
    }
  }

  async function saveEdit() {
    if (!edit.draft) return
    setError(null)
    try {
      await api.put(`/api/investments/${edit.editingId}`, toRequest(edit.draft))
      edit.cancelEdit()
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save changes.')
    }
  }

  async function remove(id: string) {
    await api.del(`/api/investments/${id}`)
    load()
  }

  const total = investments.reduce((s, i) => s + i.currentValue, 0)

  return (
    <>
      <h1>Investments &amp; pension pots</h1>
      <p className="lede">Manual valuations of your pots. Total: {gbp.format(total)}</p>
      {error && <div className="error-box">{error}</div>}

      <div className="card">
        {investments.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Type</th>
                <th className="num">Current value</th>
                <th className="num">Projected growth</th>
                <th className="num">Payday contribution</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {investments.map((i) =>
                edit.isEditing(i.id) && edit.draft ? (
                  <tr key={i.id} className="editing-row">
                    <td>
                      <input value={edit.draft.name}
                        onChange={(e) => edit.updateDraft({ name: e.target.value })} />
                    </td>
                    <td>
                      <select value={edit.draft.type}
                        onChange={(e) => {
                          const nextType = e.target.value as InvestmentType
                          edit.updateDraft({
                            type: nextType,
                            receivesIncomePensionContributions:
                              nextType === 'PensionPot' && edit.draft!.receivesIncomePensionContributions,
                          })
                        }}>
                        {TYPES.map((t) => (
                          <option key={t.value} value={t.value}>{t.label}</option>
                        ))}
                      </select>
                    </td>
                    <td className="num">
                      <input type="number" min="0" step="0.01" value={edit.draft.currentValue}
                        onChange={(e) => edit.updateDraft({ currentValue: Number(e.target.value) })} />
                    </td>
                    <td className="num">
                      <input type="number" min="-50" max="50" step="0.1"
                        value={edit.draft.expectedAnnualGrowthPercent ?? ''}
                        onChange={(e) => edit.updateDraft({
                          expectedAnnualGrowthPercent: e.target.value ? Number(e.target.value) : null,
                        })} />
                    </td>
                    <td className="num">
                      {edit.draft.type === 'PensionPot' ? (
                        <label style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', justifyContent: 'flex-end' }}>
                          <input type="checkbox" checked={edit.draft.receivesIncomePensionContributions}
                            onChange={(e) => edit.updateDraft({ receivesIncomePensionContributions: e.target.checked })} />
                          Auto
                        </label>
                      ) : '—'}
                    </td>
                    <td className="num">
                      <div className="row-actions">
                        <button onClick={saveEdit}>Save</button>
                        <button className="secondary" onClick={edit.cancelEdit}>Cancel</button>
                      </div>
                    </td>
                  </tr>
                ) : (
                  <tr key={i.id}>
                    <td>{i.name}</td>
                    <td>{typeLabel(i.type)}</td>
                    <td className="num">{gbp.format(i.currentValue)}</td>
                    <td className="num">
                      {i.expectedAnnualGrowthPercent != null ? `${i.expectedAnnualGrowthPercent}%/yr` : '—'}
                    </td>
                    <td className="num">
                      {i.receivesIncomePensionContributions ? (
                        <span className="badge">Auto</span>
                      ) : '—'}
                    </td>
                    <td className="num">
                      <div className="row-actions">
                        <button className="secondary" onClick={() => edit.startEdit(i)}>Edit</button>
                        <button className="danger" onClick={() => remove(i.id)}>Remove</button>
                      </div>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        )}
        <form className="grid" onSubmit={add} style={{ marginTop: investments.length ? '1rem' : 0 }}>
          <div className="field">
            <label>Name</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </div>
          <div className="field">
            <label>Type</label>
            <select value={type} onChange={(e) => {
              const nextType = e.target.value as InvestmentType
              setType(nextType)
              if (nextType !== 'PensionPot') setPensionContribution(false)
            }}>
              {TYPES.map((t) => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Current value (£)</label>
            <input type="number" min="0" step="0.01" value={value}
              onChange={(e) => setValue(e.target.value)} required />
          </div>
          <div className="field">
            <label>Projected growth (%/yr, optional)</label>
            <input type="number" min="-50" max="50" step="0.1" value={growth}
              onChange={(e) => setGrowth(e.target.value)} />
          </div>
          {type === 'PensionPot' && (
            <div className="field checkbox">
              <input id="pension-contribution" type="checkbox" checked={pensionContribution}
                onChange={(e) => setPensionContribution(e.target.checked)} />
              <label htmlFor="pension-contribution">Automatically add my pension contributions here</label>
            </div>
          )}
          <button type="submit">Add investment</button>
          {type === 'PensionPot' && pensionContribution && (
            <p className="muted" style={{ flexBasis: '100%', margin: 0 }}>
              Uses the employee + employer pension % from your Income page every payday. Only one pension
              pot can be linked at a time — enabling this will unlink any other.
            </p>
          )}
        </form>
      </div>
    </>
  )
}
