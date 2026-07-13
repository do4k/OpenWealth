import { useEffect, useState } from 'react'
import { api } from '../api'
import type { IncomeDetails, PensionMethod } from '../types'

const empty: IncomeDetails = {
  annualSalary: 0,
  annualBonus: 0,
  employeePensionPercent: 0,
  employerPensionPercent: 0,
  pensionMethod: 'SalarySacrifice',
  pensionOnBonus: false,
  childrenReceivingChildBenefit: 0,
}

export default function IncomePage() {
  const [income, setIncome] = useState<IncomeDetails>(empty)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.get<IncomeDetails | undefined>('/api/income')
      .then((data) => data && setIncome(data))
      .catch((e) => setError(e.message))
  }, [])

  async function save(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSaved(false)
    try {
      await api.put('/api/income', income)
      setSaved(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    }
  }

  const set = (patch: Partial<IncomeDetails>) => {
    setIncome((v) => ({ ...v, ...patch }))
    setSaved(false)
  }

  return (
    <>
      <h1>Income</h1>
      <p className="lede">
        Salary, bonus and pension. Tax and National Insurance are calculated automatically
        from your <a href="/tax-settings">tax settings</a>.
      </p>
      {error && <div className="error-box">{error}</div>}
      {saved && <div className="success-box">Saved — the dashboard now reflects your income.</div>}

      <div className="card">
        <form className="grid" onSubmit={save}>
          <div className="field">
            <label>Annual salary (£)</label>
            <input
              type="number" min="0" step="0.01" value={income.annualSalary}
              onChange={(e) => set({ annualSalary: Number(e.target.value) })}
            />
          </div>
          <div className="field">
            <label>Annual bonus (£)</label>
            <input
              type="number" min="0" step="0.01" value={income.annualBonus}
              onChange={(e) => set({ annualBonus: Number(e.target.value) })}
            />
          </div>
          <div className="field">
            <label>Your pension contribution (%)</label>
            <input
              type="number" min="0" max="100" step="0.1" value={income.employeePensionPercent}
              onChange={(e) => set({ employeePensionPercent: Number(e.target.value) })}
            />
          </div>
          <div className="field">
            <label>Employer pension contribution (%)</label>
            <input
              type="number" min="0" max="100" step="0.1" value={income.employerPensionPercent}
              onChange={(e) => set({ employerPensionPercent: Number(e.target.value) })}
            />
          </div>
          <div className="field">
            <label>Pension arrangement</label>
            <select
              value={income.pensionMethod}
              onChange={(e) => set({ pensionMethod: e.target.value as PensionMethod })}
            >
              <option value="SalarySacrifice">Salary sacrifice (saves tax &amp; NI)</option>
              <option value="NetPay">Net pay (saves tax)</option>
              <option value="ReliefAtSource">Relief at source (from net pay)</option>
            </select>
          </div>
          <div className="field checkbox">
            <input
              id="bonus-pension" type="checkbox" checked={income.pensionOnBonus}
              onChange={(e) => set({ pensionOnBonus: e.target.checked })}
            />
            <label htmlFor="bonus-pension">Pension applies to bonus too</label>
          </div>
          <div className="field">
            <label>Children receiving child benefit</label>
            <input
              type="number" min="0" step="1" value={income.childrenReceivingChildBenefit}
              onChange={(e) => set({ childrenReceivingChildBenefit: Number(e.target.value) })}
            />
            <span className="muted">Used for the high income child benefit charge.</span>
          </div>
          <button type="submit">Save income</button>
        </form>
      </div>
    </>
  )
}
