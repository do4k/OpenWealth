import { useMemo, useState } from 'react'
import { gbp } from '../api'

export interface TrendSeriesConfig<T> {
  key: keyof T & string
  label: string
  color: string
}

// Categorical palette validated for this dark surface (CVD-safe ordering)
export const WEALTH_SERIES = [
  { key: 'netWorth', label: 'Net worth', color: '#3987e5' },
  { key: 'savings', label: 'Savings', color: '#199e70' },
  { key: 'investments', label: 'Investments', color: '#c98500' },
  { key: 'otherAssets', label: 'Other assets', color: '#d55181' },
  { key: 'mortgages', label: 'Mortgages (owed)', color: '#e66767' },
  { key: 'studentLoans', label: 'Student loans (owed)', color: '#9085e9' },
  { key: 'otherDebts', label: 'Other debts (owed)', color: '#d95926' },
] as const

export const TREND_HORIZONS = [
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

interface ChartProps<T extends { date: string }> {
  historic: T[]
  projected: T[]
  series: readonly TrendSeriesConfig<T>[]
  emptyMessage?: string
}

export function TrendChart<T extends { date: string }>({
  historic,
  projected,
  series,
  emptyMessage = 'Not enough data yet.',
}: ChartProps<T>) {
  const [hover, setHover] = useState<{ x: number; point: T; projectedPt: boolean } | null>(null)

  const W = 920
  const H = 380
  const M = { top: 16, right: 20, bottom: 30, left: 66 }
  const innerW = W - M.left - M.right
  const innerH = H - M.top - M.bottom

  const value = (p: T, key: keyof T & string): number => Number(p[key])

  const { all, xOf, yOf, ticksY, ticksX } = useMemo(() => {
    const all = [
      ...historic.map((p) => ({ ...p, projectedPt: false as const })),
      ...projected.map((p) => ({ ...p, projectedPt: true as const })),
    ]
    const times = all.map((p) => Date.parse(p.date))
    const tMin = Math.min(...(times.length ? times : [Date.now()]))
    const tMax = Math.max(...(times.length ? times : [Date.now() + 1]))
    const values = all.flatMap((p) => series.map((s) => value(p, s.key)))
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
  }, [historic, projected, series])

  if (all.length < 2) {
    return <p className="muted" style={{ padding: '2rem 0' }}>{emptyMessage}</p>
  }

  const path = (points: T[], key: keyof T & string) =>
    points.map((p, i) => `${i === 0 ? 'M' : 'L'}${xOf(p.date).toFixed(1)},${yOf(value(p, key)).toFixed(1)}`).join(' ')

  const onMove = (e: React.MouseEvent<SVGSVGElement>) => {
    const rect = e.currentTarget.getBoundingClientRect()
    const px = ((e.clientX - rect.left) / rect.width) * W
    let best = null as null | { x: number; point: T; projectedPt: boolean }
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
        aria-label="Line chart of historic and projected wealth"
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

        {series.map((s) => (
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
            {series.map((s) => (
              <circle
                key={s.key} cx={hover.x} cy={yOf(value(hover.point, s.key))} r="4"
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
          {series.map((s) => (
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
                {gbp.format(value(hover.point, s.key))}
              </span>
            </div>
          ))}
        </div>
      )}

      {/* legend */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.4rem 1.1rem', padding: '0.5rem 0 0.75rem' }}>
        {series.map((s) => (
          <span key={s.key} className="muted" style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            <span style={{ width: 14, height: 3, background: s.color, borderRadius: 2, display: 'inline-block' }} />
            {s.label}
          </span>
        ))}
      </div>
    </div>
  )
}
