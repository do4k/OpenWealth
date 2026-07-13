import { useEffect, useState } from 'react'
import { api, gbp } from '../api'
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

export default function InvestmentsPage() {
  const [investments, setInvestments] = useState<Investment[]>([])
  const [name, setName] = useState('')
  const [type, setType] = useState<InvestmentType>('StocksAndSharesIsa')
  const [value, setValue] = useState('')
  const [error, setError] = useState<string | null>(null)

  const load = () => {
    api.get<Investment[]>('/api/investments').then(setInvestments).catch((e) => setError(e.message))
  }
  useEffect(load, [])

  async function add(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/investments', { name, type, currentValue: Number(value) })
      setName(''); setValue('')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add investment.')
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
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {investments.map((i) => (
                <tr key={i.id}>
                  <td>{i.name}</td>
                  <td>{typeLabel(i.type)}</td>
                  <td className="num">{gbp.format(i.currentValue)}</td>
                  <td className="num">
                    <div className="row-actions">
                      <button className="danger" onClick={() => remove(i.id)}>Remove</button>
                    </div>
                  </td>
                </tr>
              ))}
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
            <select value={type} onChange={(e) => setType(e.target.value as InvestmentType)}>
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
          <button type="submit">Add investment</button>
        </form>
      </div>
    </>
  )
}
