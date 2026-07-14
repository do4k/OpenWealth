import { useEffect, useState } from 'react'
import { api, gbp } from '../api'
import { useInlineEdit } from '../hooks/useInlineEdit'
import type { CustomAsset, CustomDebt } from '../types'

function assetRequest(a: CustomAsset) {
  return { name: a.name, value: a.value, expectedAnnualGrowthPercent: a.expectedAnnualGrowthPercent }
}

function debtRequest(d: CustomDebt) {
  return {
    name: d.name,
    balance: d.balance,
    annualInterestRatePercent: d.annualInterestRatePercent,
    monthlyPayment: d.monthlyPayment,
  }
}

export default function OtherItemsPage() {
  const [assets, setAssets] = useState<CustomAsset[]>([])
  const [debts, setDebts] = useState<CustomDebt[]>([])
  const [error, setError] = useState<string | null>(null)

  const [assetName, setAssetName] = useState('')
  const [assetValue, setAssetValue] = useState('')
  const [assetGrowth, setAssetGrowth] = useState('')
  const assetEdit = useInlineEdit<CustomAsset>()

  const [debtName, setDebtName] = useState('')
  const [debtBalance, setDebtBalance] = useState('')
  const [debtRate, setDebtRate] = useState('')
  const [debtPayment, setDebtPayment] = useState('')
  const debtEdit = useInlineEdit<CustomDebt>()

  const load = () => {
    api.get<CustomAsset[]>('/api/custom-assets').then(setAssets).catch((e) => setError(e.message))
    api.get<CustomDebt[]>('/api/custom-debts').then(setDebts).catch((e) => setError(e.message))
  }
  useEffect(load, [])

  async function addAsset(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/custom-assets', {
        name: assetName,
        value: Number(assetValue),
        expectedAnnualGrowthPercent: assetGrowth ? Number(assetGrowth) : null,
      })
      setAssetName(''); setAssetValue(''); setAssetGrowth('')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add asset.')
    }
  }

  async function saveAsset() {
    if (!assetEdit.draft) return
    setError(null)
    try {
      await api.put(`/api/custom-assets/${assetEdit.editingId}`, assetRequest(assetEdit.draft))
      assetEdit.cancelEdit()
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save asset.')
    }
  }

  async function removeAsset(id: string) {
    await api.del(`/api/custom-assets/${id}`)
    load()
  }

  async function addDebt(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/custom-debts', {
        name: debtName,
        balance: Number(debtBalance),
        annualInterestRatePercent: debtRate ? Number(debtRate) : null,
        monthlyPayment: debtPayment ? Number(debtPayment) : null,
      })
      setDebtName(''); setDebtBalance(''); setDebtRate(''); setDebtPayment('')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add debt.')
    }
  }

  async function saveDebt() {
    if (!debtEdit.draft) return
    setError(null)
    try {
      await api.put(`/api/custom-debts/${debtEdit.editingId}`, debtRequest(debtEdit.draft))
      debtEdit.cancelEdit()
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save debt.')
    }
  }

  async function removeDebt(id: string) {
    await api.del(`/api/custom-debts/${id}`)
    load()
  }

  const assetTotal = assets.reduce((s, a) => s + a.value, 0)
  const debtTotal = debts.reduce((s, d) => s + d.balance, 0)

  return (
    <>
      <h1>Other assets &amp; debts</h1>
      <p className="lede">
        Anything that doesn't fit the other categories — a car, jewellery or a business stake
        as an asset; a credit card, car finance or personal loan as a debt.
      </p>
      {error && <div className="error-box">{error}</div>}

      <div className="card">
        <h2>Other assets</h2>
        <p className="muted">Total: {gbp.format(assetTotal)}</p>
        {assets.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th className="num">Value</th>
                <th className="num">Projected growth</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {assets.map((a) =>
                assetEdit.isEditing(a.id) && assetEdit.draft ? (
                  <tr key={a.id} className="editing-row">
                    <td>
                      <input value={assetEdit.draft.name}
                        onChange={(e) => assetEdit.updateDraft({ name: e.target.value })} />
                    </td>
                    <td className="num">
                      <input type="number" min="0" step="0.01" value={assetEdit.draft.value}
                        onChange={(e) => assetEdit.updateDraft({ value: Number(e.target.value) })} />
                    </td>
                    <td className="num">
                      <input type="number" min="-100" max="100" step="0.1"
                        value={assetEdit.draft.expectedAnnualGrowthPercent ?? ''}
                        onChange={(e) => assetEdit.updateDraft({
                          expectedAnnualGrowthPercent: e.target.value ? Number(e.target.value) : null,
                        })} />
                    </td>
                    <td className="num">
                      <div className="row-actions">
                        <button onClick={saveAsset}>Save</button>
                        <button className="secondary" onClick={assetEdit.cancelEdit}>Cancel</button>
                      </div>
                    </td>
                  </tr>
                ) : (
                  <tr key={a.id}>
                    <td>{a.name}</td>
                    <td className="num">{gbp.format(a.value)}</td>
                    <td className="num">
                      {a.expectedAnnualGrowthPercent != null ? `${a.expectedAnnualGrowthPercent}%/yr` : '—'}
                    </td>
                    <td className="num">
                      <div className="row-actions">
                        <button className="secondary" onClick={() => assetEdit.startEdit(a)}>Edit</button>
                        <button className="danger" onClick={() => removeAsset(a.id)}>Remove</button>
                      </div>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        )}
        <form className="grid" onSubmit={addAsset} style={{ marginTop: assets.length ? '1rem' : 0 }}>
          <div className="field">
            <label>Name</label>
            <input value={assetName} onChange={(e) => setAssetName(e.target.value)}
              placeholder="e.g. Car" required />
          </div>
          <div className="field">
            <label>Value (£)</label>
            <input type="number" min="0" step="0.01" value={assetValue}
              onChange={(e) => setAssetValue(e.target.value)} required />
          </div>
          <div className="field">
            <label>Projected growth (%/yr, optional)</label>
            <input type="number" min="-100" max="100" step="0.1" value={assetGrowth}
              onChange={(e) => setAssetGrowth(e.target.value)} />
            <span className="muted">Negative for depreciating things like cars.</span>
          </div>
          <button type="submit">Add asset</button>
        </form>
      </div>

      <div className="card">
        <h2>Other debts</h2>
        <p className="muted">Total: {gbp.format(debtTotal)}</p>
        {debts.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th className="num">Balance</th>
                <th className="num">Interest rate</th>
                <th className="num">Monthly payment</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {debts.map((d) =>
                debtEdit.isEditing(d.id) && debtEdit.draft ? (
                  <tr key={d.id} className="editing-row">
                    <td>
                      <input value={debtEdit.draft.name}
                        onChange={(e) => debtEdit.updateDraft({ name: e.target.value })} />
                    </td>
                    <td className="num">
                      <input type="number" min="0" step="0.01" value={debtEdit.draft.balance}
                        onChange={(e) => debtEdit.updateDraft({ balance: Number(e.target.value) })} />
                    </td>
                    <td className="num">
                      <input type="number" min="0" max="100" step="0.01"
                        value={debtEdit.draft.annualInterestRatePercent ?? ''}
                        onChange={(e) => debtEdit.updateDraft({
                          annualInterestRatePercent: e.target.value ? Number(e.target.value) : null,
                        })} />
                    </td>
                    <td className="num">
                      <input type="number" min="0" step="0.01" value={debtEdit.draft.monthlyPayment ?? ''}
                        onChange={(e) => debtEdit.updateDraft({
                          monthlyPayment: e.target.value ? Number(e.target.value) : null,
                        })} />
                    </td>
                    <td className="num">
                      <div className="row-actions">
                        <button onClick={saveDebt}>Save</button>
                        <button className="secondary" onClick={debtEdit.cancelEdit}>Cancel</button>
                      </div>
                    </td>
                  </tr>
                ) : (
                  <tr key={d.id}>
                    <td>{d.name}</td>
                    <td className="num">{gbp.format(d.balance)}</td>
                    <td className="num">
                      {d.annualInterestRatePercent != null ? `${d.annualInterestRatePercent}%` : '—'}
                    </td>
                    <td className="num">
                      {d.monthlyPayment != null ? gbp.format(d.monthlyPayment) : '—'}
                    </td>
                    <td className="num">
                      <div className="row-actions">
                        <button className="secondary" onClick={() => debtEdit.startEdit(d)}>Edit</button>
                        <button className="danger" onClick={() => removeDebt(d.id)}>Remove</button>
                      </div>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        )}
        <form className="grid" onSubmit={addDebt} style={{ marginTop: debts.length ? '1rem' : 0 }}>
          <div className="field">
            <label>Name</label>
            <input value={debtName} onChange={(e) => setDebtName(e.target.value)}
              placeholder="e.g. Credit card" required />
          </div>
          <div className="field">
            <label>Balance (£)</label>
            <input type="number" min="0" step="0.01" value={debtBalance}
              onChange={(e) => setDebtBalance(e.target.value)} required />
          </div>
          <div className="field">
            <label>Interest rate (%, optional)</label>
            <input type="number" min="0" max="100" step="0.01" value={debtRate}
              onChange={(e) => setDebtRate(e.target.value)} />
          </div>
          <div className="field">
            <label>Monthly payment (£, optional)</label>
            <input type="number" min="0" step="0.01" value={debtPayment}
              onChange={(e) => setDebtPayment(e.target.value)} />
            <span className="muted">Applied every payday alongside its interest.</span>
          </div>
          <button type="submit">Add debt</button>
        </form>
      </div>
    </>
  )
}
