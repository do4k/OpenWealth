import { useEffect, useState } from 'react'
import { api, gbp, gbpExact } from '../api'
import type { Mortgage, MortgageRateType, Property } from '../types'

interface MortgageForm {
  name: string
  propertyId: string
  outstandingBalance: string
  annualInterestRatePercent: string
  rateType: MortgageRateType
  fixedRateEndDate: string
  followOnRatePercent: string
  termMonthsRemaining: string
}

const emptyMortgage: MortgageForm = {
  name: '',
  propertyId: '',
  outstandingBalance: '',
  annualInterestRatePercent: '',
  rateType: 'Fixed',
  fixedRateEndDate: '',
  followOnRatePercent: '',
  termMonthsRemaining: '',
}

export default function MortgagesPage() {
  const [properties, setProperties] = useState<Property[]>([])
  const [mortgages, setMortgages] = useState<Mortgage[]>([])
  const [propName, setPropName] = useState('')
  const [propValue, setPropValue] = useState('')
  const [form, setForm] = useState<MortgageForm>(emptyMortgage)
  const [error, setError] = useState<string | null>(null)

  const load = () => {
    api.get<Property[]>('/api/properties').then(setProperties).catch((e) => setError(e.message))
    api.get<Mortgage[]>('/api/mortgages').then(setMortgages).catch((e) => setError(e.message))
  }
  useEffect(load, [])

  async function addProperty(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/properties', { name: propName, estimatedValue: Number(propValue) })
      setPropName('')
      setPropValue('')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add property.')
    }
  }

  async function addMortgage(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/mortgages', {
        name: form.name,
        propertyId: form.propertyId || null,
        outstandingBalance: Number(form.outstandingBalance),
        annualInterestRatePercent: Number(form.annualInterestRatePercent),
        rateType: form.rateType,
        fixedRateEndDate: form.rateType === 'Fixed' && form.fixedRateEndDate ? form.fixedRateEndDate : null,
        followOnRatePercent:
          form.rateType === 'Fixed' && form.followOnRatePercent ? Number(form.followOnRatePercent) : null,
        termMonthsRemaining: Number(form.termMonthsRemaining),
      })
      setForm(emptyMortgage)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add mortgage.')
    }
  }

  async function removeProperty(id: string) {
    await api.del(`/api/properties/${id}`)
    load()
  }

  async function removeMortgage(id: string) {
    await api.del(`/api/mortgages/${id}`)
    load()
  }

  const set = (patch: Partial<MortgageForm>) => setForm((f) => ({ ...f, ...patch }))
  const propertyName = (id: string | null) => properties.find((p) => p.id === id)?.name ?? '—'

  const equity = (p: Property) =>
    p.estimatedValue - mortgages.filter((m) => m.propertyId === p.id).reduce((s, m) => s + m.outstandingBalance, 0)

  return (
    <>
      <h1>Mortgages &amp; property</h1>
      <p className="lede">
        Each mortgage has its own rate and term — fixed deals warn you when they're about to
        roll onto a variable rate.
      </p>
      {error && <div className="error-box">{error}</div>}

      <div className="card">
        <h2>Properties</h2>
        {properties.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th className="num">Estimated value</th>
                <th className="num">Equity</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {properties.map((p) => (
                <tr key={p.id}>
                  <td>{p.name}</td>
                  <td className="num">{gbp.format(p.estimatedValue)}</td>
                  <td className="num">{gbp.format(equity(p))}</td>
                  <td className="num">
                    <div className="row-actions">
                      <button className="danger" onClick={() => removeProperty(p.id)}>Remove</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <form className="grid" onSubmit={addProperty} style={{ marginTop: properties.length ? '1rem' : 0 }}>
          <div className="field">
            <label>Property name</label>
            <input value={propName} onChange={(e) => setPropName(e.target.value)} required />
          </div>
          <div className="field">
            <label>Estimated value (£)</label>
            <input type="number" min="0" step="1000" value={propValue}
              onChange={(e) => setPropValue(e.target.value)} required />
          </div>
          <button type="submit">Add property</button>
        </form>
      </div>

      <div className="card">
        <h2>Mortgages</h2>
        {mortgages.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Property</th>
                <th className="num">Balance</th>
                <th className="num">Rate</th>
                <th>Type</th>
                <th className="num">Term left</th>
                <th className="num">Monthly</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {mortgages.map((m) => (
                <tr key={m.id}>
                  <td>{m.name}</td>
                  <td>{propertyName(m.propertyId)}</td>
                  <td className="num">{gbp.format(m.outstandingBalance)}</td>
                  <td className="num">{m.annualInterestRatePercent}%</td>
                  <td>
                    {m.rateType === 'Fixed' ? (
                      m.isFixedPeriodOver ? (
                        <span className="badge warn">Fix ended — now variable</span>
                      ) : (
                        <span className="badge">
                          Fixed{m.fixedRateEndDate ? ` until ${m.fixedRateEndDate}` : ''}
                        </span>
                      )
                    ) : (
                      <span className="badge warn">Variable</span>
                    )}
                    {m.rateType === 'Fixed' && m.followOnRatePercent != null && !m.isFixedPeriodOver && (
                      <span className="muted"> then {m.followOnRatePercent}%</span>
                    )}
                  </td>
                  <td className="num">{Math.floor(m.termMonthsRemaining / 12)}y {m.termMonthsRemaining % 12}m</td>
                  <td className="num">{gbpExact.format(m.monthlyPayment)}</td>
                  <td className="num">
                    <div className="row-actions">
                      <button className="danger" onClick={() => removeMortgage(m.id)}>Remove</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <form className="grid" onSubmit={addMortgage} style={{ marginTop: mortgages.length ? '1rem' : 0 }}>
          <div className="field">
            <label>Name / lender</label>
            <input value={form.name} onChange={(e) => set({ name: e.target.value })} required />
          </div>
          <div className="field">
            <label>Linked property</label>
            <select value={form.propertyId} onChange={(e) => set({ propertyId: e.target.value })}>
              <option value="">None</option>
              {properties.map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Outstanding balance (£)</label>
            <input type="number" min="0" step="0.01" value={form.outstandingBalance}
              onChange={(e) => set({ outstandingBalance: e.target.value })} required />
          </div>
          <div className="field">
            <label>Interest rate (%)</label>
            <input type="number" min="0" max="30" step="0.01" value={form.annualInterestRatePercent}
              onChange={(e) => set({ annualInterestRatePercent: e.target.value })} required />
          </div>
          <div className="field">
            <label>Rate type</label>
            <select value={form.rateType}
              onChange={(e) => set({ rateType: e.target.value as MortgageRateType })}>
              <option value="Fixed">Fixed</option>
              <option value="Variable">Variable</option>
            </select>
          </div>
          {form.rateType === 'Fixed' && (
            <>
              <div className="field">
                <label>Fixed until</label>
                <input type="date" value={form.fixedRateEndDate}
                  onChange={(e) => set({ fixedRateEndDate: e.target.value })} />
              </div>
              <div className="field">
                <label>Follow-on rate (%, optional)</label>
                <input type="number" min="0" max="30" step="0.01" value={form.followOnRatePercent}
                  onChange={(e) => set({ followOnRatePercent: e.target.value })} />
              </div>
            </>
          )}
          <div className="field">
            <label>Term remaining (months)</label>
            <input type="number" min="1" step="1" value={form.termMonthsRemaining}
              onChange={(e) => set({ termMonthsRemaining: e.target.value })} required />
          </div>
          <button type="submit">Add mortgage</button>
        </form>
      </div>
    </>
  )
}
