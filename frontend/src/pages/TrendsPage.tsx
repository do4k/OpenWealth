import { useEffect, useState } from 'react'
import { api, gbp, gbpExact } from '../api'
import { TrendChart, TREND_HORIZONS, WEALTH_SERIES } from '../components/TrendChart'
import type { AccrualEvent, AutomationSettings, HistoryResponse, WealthPoint } from '../types'

const monthYear = (iso: string) =>
  new Date(iso).toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })

export default function TrendsPage() {
  const [automation, setAutomation] = useState<AutomationSettings | null>(null)
  const [history, setHistory] = useState<HistoryResponse | null>(null)
  const [projection, setProjection] = useState<WealthPoint[]>([])
  const [months, setMonths] = useState(60)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [showTable, setShowTable] = useState(false)

  const loadData = (horizon: number) => {
    api.get<HistoryResponse>('/api/history').then(setHistory).catch((e) => setError(e.message))
    api.get<WealthPoint[]>(`/api/projections?months=${horizon}`)
      .then(setProjection).catch((e) => setError(e.message))
  }

  useEffect(() => {
    api.get<AutomationSettings>('/api/automation').then(setAutomation).catch((e) => setError(e.message))
    loadData(months)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const changeHorizon = (m: number) => {
    setMonths(m)
    loadData(m)
  }

  async function saveAutomation(next: AutomationSettings) {
    setError(null)
    setNotice(null)
    try {
      const saved = await api.put<AutomationSettings>('/api/automation', {
        enabled: next.enabled,
        paydayDayOfMonth: next.paydayDayOfMonth,
      })
      setAutomation(saved)
      setNotice(saved.enabled
        ? 'Automation on — interest and repayments will be applied every payday.'
        : 'Automation switched off.')
      loadData(months)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    }
  }

  async function runNow() {
    setError(null)
    setNotice(null)
    try {
      const res = await api.post<{ paydaysApplied: number }>('/api/automation/run-now')
      setNotice(res.paydaysApplied > 0
        ? `Applied ${res.paydaysApplied} payday(s).`
        : 'Nothing due — the next payday hasn\'t arrived yet.')
      loadData(months)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to run.')
    }
  }

  return (
    <>
      <h1>Trends</h1>
      <p className="lede">
        Historic net worth recorded every payday, and where the same rules take you next —
        savings compounding up, mortgages and loans amortising down.
      </p>
      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}

      {automation && (
        <div className="card">
          <h2>Payday automation</h2>
          <p className="muted">
            On payday, savings and ISAs earn a month of their interest, student loans accrue
            their global plan rate and get a month's repayment, and mortgages accrue interest
            and pay their amortised monthly payment. Missed paydays are caught up automatically.
          </p>
          <form
            className="grid"
            onSubmit={(e) => {
              e.preventDefault()
              saveAutomation(automation)
            }}
          >
            <div className="field checkbox">
              <input
                id="auto-enabled" type="checkbox" checked={automation.enabled}
                onChange={(e) => setAutomation({ ...automation, enabled: e.target.checked })}
              />
              <label htmlFor="auto-enabled">Apply interest &amp; repayments automatically</label>
            </div>
            <div className="field">
              <label>Payday (day of month)</label>
              <input
                type="number" min="1" max="31" value={automation.paydayDayOfMonth}
                onChange={(e) =>
                  setAutomation({ ...automation, paydayDayOfMonth: Number(e.target.value) })}
              />
            </div>
            <button type="submit">Save</button>
            <button type="button" className="secondary" onClick={runNow}
              disabled={!automation.enabled}>
              Run payday now
            </button>
          </form>
          {automation.lastAccrualDate && (
            <p className="muted" style={{ marginBottom: 0 }}>
              Last applied: {automation.lastAccrualDate}
            </p>
          )}
        </div>
      )}

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
                onClick={() => changeHorizon(h.months)}
              >
                {h.label}
              </button>
            ))}
          </div>
        </div>
        <TrendChart
          historic={history?.snapshots ?? []}
          projected={projection}
          series={WEALTH_SERIES}
          emptyMessage="Not enough data yet — enable payday automation above to start recording history,
            or add balances and rates so projections have something to work from."
        />
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
          <p className="muted" style={{ margin: 0 }}>
            Solid lines are recorded history; dashed lines are projected from today's balances,
            rates and repayments. Liabilities are shown as the amount owed.
          </p>
          <button type="button" className="link-button" onClick={() => setShowTable((v) => !v)}>
            {showTable ? 'Hide data table' : 'Show data table'}
          </button>
        </div>
        {showTable && <DataTable historic={history?.snapshots ?? []} projected={projection} />}
      </div>

      {history && history.events.length > 0 && <EventsCard events={history.events} />}
    </>
  )
}

interface ChartProps {
  historic: WealthPoint[]
  projected: WealthPoint[]
}

function DataTable({ historic, projected }: ChartProps) {
  const rows = [
    ...historic.map((p) => ({ ...p, kind: 'History' })),
    ...projected.filter((_, i) => i % 12 === 0).map((p) => ({ ...p, kind: 'Projected' })),
  ]
  return (
    <div style={{ overflowX: 'auto', marginTop: '0.75rem' }}>
      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th></th>
            <th className="num">Net worth</th>
            <th className="num">Savings</th>
            <th className="num">Investments</th>
            <th className="num">Other assets</th>
            <th className="num">Mortgages</th>
            <th className="num">Student loans</th>
            <th className="num">Other debts</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r, i) => (
            <tr key={i}>
              <td>{monthYear(r.date)}</td>
              <td className="muted">{r.kind}</td>
              <td className="num">{gbp.format(r.netWorth)}</td>
              <td className="num">{gbp.format(r.savings)}</td>
              <td className="num">{gbp.format(r.investments)}</td>
              <td className="num">{gbp.format(r.otherAssets)}</td>
              <td className="num">{gbp.format(r.mortgages)}</td>
              <td className="num">{gbp.format(r.studentLoans)}</td>
              <td className="num">{gbp.format(r.otherDebts)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function EventsCard({ events }: { events: AccrualEvent[] }) {
  return (
    <div className="card">
      <h2>Payday history</h2>
      <p className="muted">Every automatic balance change, so you can audit what was applied.</p>
      <div style={{ overflowX: 'auto' }}>
        <table>
          <thead>
            <tr>
              <th>Date</th>
              <th>Item</th>
              <th className="num">Interest</th>
              <th className="num">Deposit</th>
              <th className="num">Payment</th>
              <th className="num">New balance</th>
            </tr>
          </thead>
          <tbody>
            {events.slice(0, 30).map((e, i) => (
              <tr key={i}>
                <td>{e.date}</td>
                <td>{e.category}: {e.itemName}</td>
                <td className="num">+{gbpExact.format(e.interestAmount)}</td>
                <td className="num">
                  {e.depositAmount > 0
                    ? `+${gbpExact.format(e.depositAmount)}`
                    : e.depositAmount < 0
                      ? `−${gbpExact.format(-e.depositAmount)}`
                      : '—'}
                </td>
                <td className="num">{e.paymentAmount > 0 ? `−${gbpExact.format(e.paymentAmount)}` : '—'}</td>
                <td className="num">{gbpExact.format(e.newBalance)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
