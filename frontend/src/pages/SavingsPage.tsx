import { useEffect, useState } from 'react'
import { api, gbp } from '../api'
import { useInlineEdit } from '../hooks/useInlineEdit'
import type { SavingsAccount, SavingsAccountType } from '../types'

const TYPES: { value: SavingsAccountType; label: string }[] = [
  { value: 'CurrentAccount', label: 'Current account' },
  { value: 'EasyAccess', label: 'Easy access savings' },
  { value: 'FixedTerm', label: 'Fixed term savings' },
  { value: 'CashIsa', label: 'Cash ISA' },
  { value: 'PremiumBonds', label: 'Premium Bonds' },
  { value: 'Other', label: 'Other' },
]

const typeLabel = (t: SavingsAccountType) => TYPES.find((x) => x.value === t)?.label ?? t

function toRequest(a: SavingsAccount) {
  return {
    name: a.name,
    type: a.type,
    balance: a.balance,
    annualInterestRatePercent: a.annualInterestRatePercent,
    monthlyDeposit: a.monthlyDeposit,
  }
}

export default function SavingsPage() {
  const [accounts, setAccounts] = useState<SavingsAccount[]>([])
  const [name, setName] = useState('')
  const [type, setType] = useState<SavingsAccountType>('EasyAccess')
  const [balance, setBalance] = useState('')
  const [rate, setRate] = useState('')
  const [deposit, setDeposit] = useState('')
  const [error, setError] = useState<string | null>(null)
  const edit = useInlineEdit<SavingsAccount>()

  const load = () => {
    api.get<SavingsAccount[]>('/api/savings').then(setAccounts).catch((e) => setError(e.message))
  }
  useEffect(load, [])

  async function add(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/savings', {
        name,
        type,
        balance: Number(balance),
        annualInterestRatePercent: rate ? Number(rate) : null,
        monthlyDeposit: deposit ? Number(deposit) : 0,
      })
      setName(''); setBalance(''); setRate(''); setDeposit('')
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add account.')
    }
  }

  async function saveEdit() {
    if (!edit.draft) return
    setError(null)
    try {
      await api.put(`/api/savings/${edit.editingId}`, toRequest(edit.draft))
      edit.cancelEdit()
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save changes.')
    }
  }

  async function remove(id: string) {
    await api.del(`/api/savings/${id}`)
    load()
  }

  const total = accounts.reduce((s, a) => s + a.balance, 0)

  return (
    <>
      <h1>Savings &amp; cash</h1>
      <p className="lede">Cash accounts, ISAs and Premium Bonds. Total: {gbp.format(total)}</p>
      {error && <div className="error-box">{error}</div>}

      <div className="card">
        {accounts.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Type</th>
                <th className="num">Balance</th>
                <th className="num">Rate</th>
                <th className="num">Monthly deposit</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {accounts.map((a) =>
                edit.isEditing(a.id) && edit.draft ? (
                  <tr key={a.id} className="editing-row">
                    <td>
                      <input value={edit.draft.name}
                        onChange={(e) => edit.updateDraft({ name: e.target.value })} />
                    </td>
                    <td>
                      <select value={edit.draft.type}
                        onChange={(e) => edit.updateDraft({ type: e.target.value as SavingsAccountType })}>
                        {TYPES.map((t) => (
                          <option key={t.value} value={t.value}>{t.label}</option>
                        ))}
                      </select>
                    </td>
                    <td className="num">
                      <input type="number" min="0" step="0.01" value={edit.draft.balance}
                        onChange={(e) => edit.updateDraft({ balance: Number(e.target.value) })} />
                    </td>
                    <td className="num">
                      <input type="number" min="0" max="100" step="0.01"
                        value={edit.draft.annualInterestRatePercent ?? ''}
                        onChange={(e) => edit.updateDraft({
                          annualInterestRatePercent: e.target.value ? Number(e.target.value) : null,
                        })} />
                    </td>
                    <td className="num">
                      <input type="number" step="0.01" value={edit.draft.monthlyDeposit}
                        onChange={(e) => edit.updateDraft({ monthlyDeposit: Number(e.target.value) })} />
                    </td>
                    <td className="num">
                      <div className="row-actions">
                        <button onClick={saveEdit}>Save</button>
                        <button className="secondary" onClick={edit.cancelEdit}>Cancel</button>
                      </div>
                    </td>
                  </tr>
                ) : (
                  <tr key={a.id}>
                    <td>{a.name}</td>
                    <td>{typeLabel(a.type)}</td>
                    <td className="num">{gbp.format(a.balance)}</td>
                    <td className="num">{a.annualInterestRatePercent != null ? `${a.annualInterestRatePercent}%` : '—'}</td>
                    <td className="num">{a.monthlyDeposit !== 0 ? gbp.format(a.monthlyDeposit) : '—'}</td>
                    <td className="num">
                      <div className="row-actions">
                        <button className="secondary" onClick={() => edit.startEdit(a)}>Edit</button>
                        <button className="danger" onClick={() => remove(a.id)}>Remove</button>
                      </div>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        )}
        <form className="grid" onSubmit={add} style={{ marginTop: accounts.length ? '1rem' : 0 }}>
          <div className="field">
            <label>Name</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </div>
          <div className="field">
            <label>Type</label>
            <select value={type} onChange={(e) => setType(e.target.value as SavingsAccountType)}>
              {TYPES.map((t) => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Balance (£)</label>
            <input type="number" min="0" step="0.01" value={balance}
              onChange={(e) => setBalance(e.target.value)} required />
          </div>
          <div className="field">
            <label>Interest rate (%, optional)</label>
            <input type="number" min="0" max="100" step="0.01" value={rate}
              onChange={(e) => setRate(e.target.value)} />
          </div>
          <div className="field">
            <label>Monthly deposit (£, optional)</label>
            <input type="number" step="0.01" value={deposit}
              onChange={(e) => setDeposit(e.target.value)} />
            <span className="muted">Added every payday; negative for a regular withdrawal.</span>
          </div>
          <button type="submit">Add account</button>
        </form>
      </div>
    </>
  )
}
