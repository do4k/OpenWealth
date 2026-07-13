import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, gbp, gbpExact } from '../api'
import type { WealthSummary } from '../types'

export default function DashboardPage() {
  const [summary, setSummary] = useState<WealthSummary | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.get<WealthSummary>('/api/summary').then(setSummary).catch((e) => setError(e.message))
  }, [])

  if (error) return <div className="error-box">{error}</div>
  if (!summary) return <p className="muted">Loading…</p>

  const th = summary.takeHome

  return (
    <>
      <h1>Dashboard</h1>
      <p className="lede">Your complete wealth picture at a glance.</p>

      <div className="stat-grid">
        <div className="stat">
          <div className="label">Net worth</div>
          <div className={`value ${summary.netWorth >= 0 ? 'positive' : 'negative'}`}>
            {gbp.format(summary.netWorth)}
          </div>
        </div>
        <div className="stat">
          <div className="label">Total assets</div>
          <div className="value">{gbp.format(summary.totalAssets)}</div>
        </div>
        <div className="stat">
          <div className="label">Total liabilities</div>
          <div className="value">{gbp.format(summary.totalLiabilities)}</div>
        </div>
        {th && (
          <div className="stat">
            <div className="label">Monthly take-home</div>
            <div className="value positive">{gbpExact.format(th.monthlyTakeHome)}</div>
          </div>
        )}
      </div>

      <div className="card">
        <h2>Breakdown</h2>
        {summary.items.length === 0 ? (
          <p className="muted">
            Nothing here yet — add your <Link to="/income">income</Link>,{' '}
            <Link to="/mortgages">property</Link>, <Link to="/savings">savings</Link> and{' '}
            <Link to="/student-loans">loans</Link> to build your picture.
          </p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Category</th>
                <th>Item</th>
                <th className="num">Value</th>
              </tr>
            </thead>
            <tbody>
              {summary.items.map((item, i) => (
                <tr key={i}>
                  <td>{item.category}</td>
                  <td>{item.name}</td>
                  <td className={`num ${item.value < 0 ? 'value negative' : ''}`}>
                    {gbp.format(item.value)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {th && (
        <div className="card">
          <h2>Annual income &amp; deductions ({gbp.format(th.grossIncome)} gross)</h2>
          <table>
            <tbody>
              <tr>
                <td>Income tax</td>
                <td className="num">−{gbpExact.format(th.incomeTax)}</td>
              </tr>
              <tr>
                <td>National Insurance</td>
                <td className="num">−{gbpExact.format(th.nationalInsurance)}</td>
              </tr>
              {th.studentLoanRepayments.map((r) => (
                <tr key={r.plan}>
                  <td>Student loan ({r.plan})</td>
                  <td className="num">−{gbpExact.format(r.annualRepayment)}</td>
                </tr>
              ))}
              <tr>
                <td>Pension (you)</td>
                <td className="num">−{gbpExact.format(th.employeePensionContribution)}</td>
              </tr>
              <tr>
                <td>Pension (employer, on top)</td>
                <td className="num">+{gbpExact.format(th.employerPensionContribution)}</td>
              </tr>
              <tr>
                <td><strong>Take-home</strong></td>
                <td className="num">
                  <strong>{gbpExact.format(th.annualTakeHome)}</strong>{' '}
                  <span className="muted">({gbpExact.format(th.monthlyTakeHome)}/month)</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
