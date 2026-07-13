import { useEffect, useState } from 'react'
import { api } from '../api'
import type { TaxSettings } from '../types'

type NumericKey = Exclude<keyof TaxSettings, 'taxYearLabel'>

const SECTIONS: { title: string; fields: { key: NumericKey; label: string }[] }[] = [
  {
    title: 'Income tax',
    fields: [
      { key: 'personalAllowance', label: 'Personal allowance (£)' },
      { key: 'personalAllowanceTaperThreshold', label: 'Allowance taper threshold (£)' },
      { key: 'basicRateLimit', label: 'Basic rate limit (£ taxable)' },
      { key: 'higherRateLimit', label: 'Higher rate limit (£ taxable)' },
      { key: 'basicRatePercent', label: 'Basic rate (%)' },
      { key: 'higherRatePercent', label: 'Higher rate (%)' },
      { key: 'additionalRatePercent', label: 'Additional rate (%)' },
    ],
  },
  {
    title: 'National Insurance (employee)',
    fields: [
      { key: 'niPrimaryThresholdAnnual', label: 'Primary threshold (£/year)' },
      { key: 'niUpperEarningsLimitAnnual', label: 'Upper earnings limit (£/year)' },
      { key: 'niMainRatePercent', label: 'Main rate (%)' },
      { key: 'niUpperRatePercent', label: 'Upper rate (%)' },
    ],
  },
]

export default function TaxSettingsPage() {
  const [settings, setSettings] = useState<TaxSettings | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    api.get<TaxSettings>('/api/settings/tax').then(setSettings).catch((e) => setError(e.message))
  }, [])

  async function save(e: React.FormEvent) {
    e.preventDefault()
    if (!settings) return
    setError(null)
    setSaved(false)
    try {
      await api.put('/api/settings/tax', settings)
      setSaved(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    }
  }

  const set = (patch: Partial<TaxSettings>) => {
    setSettings((s) => (s ? { ...s, ...patch } : s))
    setSaved(false)
  }

  if (!settings) return <p className="muted">Loading…</p>

  return (
    <>
      <h1>Tax settings</h1>
      <p className="lede">
        Rates and thresholds used for the take-home calculation. Seeded with 2025/26
        figures — update them when a new tax year starts.
      </p>
      {error && <div className="error-box">{error}</div>}
      {saved && <div className="success-box">Tax settings saved.</div>}

      <form onSubmit={save}>
        <div className="card">
          <div className="field" style={{ maxWidth: 220 }}>
            <label>Tax year</label>
            <input value={settings.taxYearLabel} onChange={(e) => set({ taxYearLabel: e.target.value })} />
          </div>
        </div>
        {SECTIONS.map((section) => (
          <div className="card" key={section.title}>
            <h2>{section.title}</h2>
            <div className="grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '0.9rem' }}>
              {section.fields.map((f) => (
                <div className="field" key={f.key}>
                  <label>{f.label}</label>
                  <input type="number" min="0" step="0.01" value={settings[f.key]}
                    onChange={(e) => set({ [f.key]: Number(e.target.value) } as Partial<TaxSettings>)} />
                </div>
              ))}
            </div>
          </div>
        ))}
        <button type="submit">Save tax settings</button>
      </form>
    </>
  )
}
