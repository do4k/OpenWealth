import { useEffect, useMemo, useState } from 'react'
import { api, gbp, gbpExact } from '../api'
import type { AccrualEvent, AutomationSettings, HistoryResponse, WealthPoint } from '../types'

// Categorical palette validated for this dark surface (CVD-safe ordering)
const SERIES = [
  { key: 'netWorth', label: 'Net worth', color: '#3987e5' },
  { key: 'savings', label: 'Savings', color: '#199e70' },
  { key: 'investments', label: 'Investments', color: '#c98500' },
  { key: 'mortgages', label: 'Mortgages (owed)', color: '#e66767' },
  { key: 'studentLoans', label: 'Student loans (owed)', color: '#9085e9' },
] as const

type SeriesKey = (typeof SERIES)[number]['key']

const HORIZONS = [
  { months: 12, label: '1 year' },
  { months: 24, label: '2 years' },
  { months: 60, label: '5 years' },
  { months: 120, label: '10 years' },
  { months: 300, label: '25 years' },
]

const compact = new Intl.NumberFormat('en-GB', {
  style: 'currency', currency: 'GBP', notation: 'compact', maximumFractionDigits: 1,
})

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
            {HORIZONS.map((h) => (
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
        <TrendChart historic={history?.snapshots ?? []} projected={projection} />
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

function TrendChart({ historic, projected }: ChartProps) {
  const [hover, setHover] = useState<{ x: number; point: WealthPoint; projectedPt: boolean } | null>(null)

  const W = 920
  const H = 380
  const M = { top: 16, right: 20, bottom: 30, left: 66 }
  const innerW = W - M.left - M.right
  const innerH = H - M.top - M.bottom

  const { all, xOf, yOf, ticksY, ticksX } = useMemo(() => {
    const all = [
      ...historic.map((p) => ({ ...p, projectedPt: false })),
      ...projected.map((p) => ({ ...p, projectedPt: true })),
    ]
    const times = all.map((p) => Date.parse(p.date))
    const tMin = Math.min(...(times.length ? times : [Date.now()]))
    const tMax = Math.max(...(times.length ? times : [Date.now() + 1]))
    const values = all.flatMap((p) => SERIES.map((s) => p[s.key]))
    const vMax = Math.max(10, ...values)
    const vMin = Math.min(0, ...values)
    const span = vMax - vMin || 1

    const xOf = (iso: string) =>
      M.left + ((Date.parse(iso) - tMin) / Math.max(1, tMax - tMin)) * innerW
    const yOf = (v: number) => M.top + innerH - ((v - vMin) / span) * innerH

    // Round y ticks to a 1/2/5 step
    const rawStep = span / 5
    const pow = 10 ** Math.floor(Math.log10(rawStep))
    const step = [1, 2, 5, 10].map((m) => m * pow).find((s) => s >= rawStep) ?? rawStep
    const ticksY: number[] = []
    for (let v = Math.ceil(vMin / step) * step; v <= vMax; v += step) ticksY.push(v)

    const tickCount = 6
    const ticksX: number[] = Array.from({ length: tickCount }, (_, i) =>
      tMin + ((tMax - tMin) * i) / (tickCount - 1))

    return { all, xOf, yOf, ticksY, ticksX }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [historic, projected])

  if (all.length < 2) {
    return (
      <p className="muted" style={{ padding: '2rem 0' }}>
        Not enough data yet — enable payday automation above to start recording history,
        or add balances and rates so projections have something to work from.
      </p>
    )
  }

  const path = (points: WealthPoint[], key: SeriesKey) =>
    points.map((p, i) => `${i === 0 ? 'M' : 'L'}${xOf(p.date).toFixed(1)},${yOf(p[key]).toFixed(1)}`).join(' ')

  const onMove = (e: React.MouseEvent<SVGSVGElement>) => {
    const rect = e.currentTarget.getBoundingClientRect()
    const px = ((e.clientX - rect.left) / rect.width) * W
    let best = null as null | { x: number; point: WealthPoint; projectedPt: boolean }
    for (const p of all) {
      const x = xOf(p.date)
      if (!best || Math.abs(x - px) < Math.abs(best.x - px))
        best = { x, point: p, projectedPt: p.projectedPt }
    }
    setHover(best)
  }

  return (
    <div style={{ position: 'relative' }}>
      <svg
        viewBox={`0 0 ${W} ${H}`}
        style={{ width: '100%', height: 'auto', display: 'block' }}
        onMouseMove={onMove}
        onMouseLeave={() => setHover(null)}
        role="img"
        aria-label="Line chart of historic and projected wealth by category"
      >
        {/* gridlines */}
        {ticksY.map((v) => (
          <g key={v}>
            <line
              x1={M.left} x2={W - M.right} y1={yOf(v)} y2={yOf(v)}
              stroke="rgba(255,255,255,0.07)" strokeWidth="1"
            />
            <text
              x={M.left - 8} y={yOf(v) + 4} textAnchor="end" fontSize="12"
              fill="var(--text-dim)" style={{ fontVariantNumeric: 'tabular-nums' }}
            >
              {compact.format(v)}
            </text>
          </g>
        ))}
        {/* x ticks: month+year for short spans, plain year once labels would repeat */}
        {ticksX.map((t, i) => {
          const spanYears = (ticksX[ticksX.length - 1] - ticksX[0]) / (365.25 * 24 * 3600 * 1000)
          const label = spanYears > 3
            ? String(new Date(t).getFullYear())
            : new Date(t).toLocaleDateString('en-GB', { month: 'short', year: 'numeric' })
          return (
            <text
              key={i} x={xOf(new Date(t).toISOString())} y={H - 8} textAnchor="middle"
              fontSize="12" fill="var(--text-dim)" style={{ fontVariantNumeric: 'tabular-nums' }}
            >
              {label}
            </text>
          )
        })}
        {/* baseline */}
        <line
          x1={M.left} x2={W - M.right} y1={yOf(0)} y2={yOf(0)}
          stroke="rgba(255,255,255,0.25)" strokeWidth="1"
        />

        {SERIES.map((s) => (
          <g key={s.key}>
            {historic.length > 1 && (
              <path d={path(historic, s.key)} fill="none" stroke={s.color} strokeWidth="2" />
            )}
            {projected.length > 1 && (
              <path
                d={path(projected, s.key)} fill="none" stroke={s.color} strokeWidth="2"
                strokeDasharray="6 5" opacity="0.9"
              />
            )}
          </g>
        ))}

        {/* hover crosshair */}
        {hover && (
          <g>
            <line
              x1={hover.x} x2={hover.x} y1={M.top} y2={H - M.bottom}
              stroke="rgba(255,255,255,0.35)" strokeWidth="1"
            />
            {SERIES.map((s) => (
              <circle
                key={s.key} cx={hover.x} cy={yOf(hover.point[s.key])} r="4"
                fill={s.color} stroke="var(--bg-raised)" strokeWidth="2"
              />
            ))}
          </g>
        )}
      </svg>

      {hover && (
        <div
          style={{
            position: 'absolute',
            left: `${Math.min(74, Math.max(2, (hover.x / W) * 100))}%`,
            top: 8,
            background: 'var(--bg-inset)',
            border: '1px solid var(--border)',
            borderRadius: 8,
            padding: '0.5rem 0.75rem',
            pointerEvents: 'none',
            fontSize: '0.82rem',
            minWidth: 210,
            whiteSpace: 'nowrap',
          }}
        >
          <div style={{ fontWeight: 600, marginBottom: 4 }}>
            {monthYear(hover.point.date)}
            {hover.projectedPt && <span className="muted"> (projected)</span>}
          </div>
          {SERIES.map((s) => (
            <div key={s.key} style={{ display: 'flex', justifyContent: 'space-between', gap: 12 }}>
              <span>
                <span
                  style={{
                    display: 'inline-block', width: 9, height: 9, borderRadius: 2,
                    background: s.color, marginRight: 6,
                  }}
                />
                {s.label}
              </span>
              <span style={{ fontVariantNumeric: 'tabular-nums' }}>
                {gbp.format(hover.point[s.key])}
              </span>
            </div>
          ))}
        </div>
      )}

      {/* legend */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.4rem 1.1rem', padding: '0.5rem 0 0.75rem' }}>
        {SERIES.map((s) => (
          <span key={s.key} className="muted" style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            <span style={{ width: 14, height: 3, background: s.color, borderRadius: 2, display: 'inline-block' }} />
            {s.label}
          </span>
        ))}
      </div>
    </div>
  )
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
            <th className="num">Mortgages</th>
            <th className="num">Student loans</th>
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
              <td className="num">{gbp.format(r.mortgages)}</td>
              <td className="num">{gbp.format(r.studentLoans)}</td>
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
