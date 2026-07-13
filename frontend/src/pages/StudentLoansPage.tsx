import { useEffect, useState } from 'react'
import { api, gbp } from '../api'
import type { StudentLoan, StudentLoanPlan, StudentLoanPlanSetting } from '../types'

const PLANS: StudentLoanPlan[] = ['Plan1', 'Plan2', 'Plan4', 'Plan5', 'Postgraduate']

const planLabel = (p: StudentLoanPlan) => (p === 'Postgraduate' ? 'Postgraduate loan' : `Plan ${p.slice(4)}`)

export default function StudentLoansPage() {
  const [loans, setLoans] = useState<StudentLoan[]>([])
  const [settings, setSettings] = useState<StudentLoanPlanSetting[]>([])
  const [plan, setPlan] = useState<StudentLoanPlan>('Plan2')
  const [balance, setBalance] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [settingsSaved, setSettingsSaved] = useState(false)

  const load = () => {
    api.get<StudentLoan[]>('/api/student-loans').then(setLoans).catch((e) => setError(e.message))
    api.get<StudentLoanPlanSetting[]>('/api/settings/student-loan-plans')
      .then(setSettings).catch((e) => setError(e.message))
  }
  useEffect(load, [])

  async function addLoan(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/student-loans', { plan, balance: Number(balance), notes: null })
      setBalance('')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add loan.')
    }
  }

  async function removeLoan(id: string) {
    await api.del(`/api/student-loans/${id}`)
    load()
  }

  async function saveSettings(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    setSettingsSaved(false)
    try {
      await api.put('/api/settings/student-loan-plans', settings)
      setSettingsSaved(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save settings.')
    }
  }

  const updateSetting = (p: StudentLoanPlan, patch: Partial<StudentLoanPlanSetting>) => {
    setSettings((all) => all.map((s) => (s.plan === p ? { ...s, ...patch } : s)))
    setSettingsSaved(false)
  }

  const settingFor = (p: StudentLoanPlan) => settings.find((s) => s.plan === p)

  return (
    <>
      <h1>Student loans</h1>
      <p className="lede">
        UK student loans by plan. Repayments are worked out from your income automatically;
        interest rates are configured globally per plan below.
      </p>
      {error && <div className="error-box">{error}</div>}

      <div className="card">
        <h2>Your loans</h2>
        {loans.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Plan</th>
                <th className="num">Balance</th>
                <th className="num">Interest rate</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {loans.map((loan) => (
                <tr key={loan.id}>
                  <td>{planLabel(loan.plan)}</td>
                  <td className="num">{gbp.format(loan.balance)}</td>
                  <td className="num">{settingFor(loan.plan)?.interestRatePercent ?? '—'}%</td>
                  <td className="num">
                    <div className="row-actions">
                      <button className="danger" onClick={() => removeLoan(loan.id)}>Remove</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <form className="grid" onSubmit={addLoan} style={{ marginTop: loans.length ? '1rem' : 0 }}>
          <div className="field">
            <label>Plan</label>
            <select value={plan} onChange={(e) => setPlan(e.target.value as StudentLoanPlan)}>
              {PLANS.map((p) => (
                <option key={p} value={p}>{planLabel(p)}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Outstanding balance (£)</label>
            <input type="number" min="0" step="0.01" value={balance}
              onChange={(e) => setBalance(e.target.value)} required />
          </div>
          <button type="submit">Add loan</button>
        </form>
      </div>

      <div className="card">
        <h2>Global plan settings</h2>
        <p className="muted">
          One interest rate per plan, applied to every loan of that plan. Thresholds and
          repayment rates feed the take-home calculation — update them each tax year.
        </p>
        {settingsSaved && <div className="success-box">Plan settings saved.</div>}
        <form onSubmit={saveSettings}>
          <table>
            <thead>
              <tr>
                <th>Plan</th>
                <th className="num">Annual threshold (£)</th>
                <th className="num">Repayment rate (%)</th>
                <th className="num">Interest rate (%)</th>
              </tr>
            </thead>
            <tbody>
              {settings.map((s) => (
                <tr key={s.plan}>
                  <td>{planLabel(s.plan)}</td>
                  <td className="num">
                    <input type="number" min="0" step="1" value={s.annualRepaymentThreshold}
                      onChange={(e) => updateSetting(s.plan, { annualRepaymentThreshold: Number(e.target.value) })} />
                  </td>
                  <td className="num">
                    <input type="number" min="0" max="100" step="0.1" value={s.repaymentRatePercent}
                      onChange={(e) => updateSetting(s.plan, { repaymentRatePercent: Number(e.target.value) })} />
                  </td>
                  <td className="num">
                    <input type="number" min="0" max="100" step="0.1" value={s.interestRatePercent}
                      onChange={(e) => updateSetting(s.plan, { interestRatePercent: Number(e.target.value) })} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <button type="submit" style={{ marginTop: '1rem' }}>Save plan settings</button>
        </form>
      </div>
    </>
  )
}
