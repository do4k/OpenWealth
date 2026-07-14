import { useEffect, useState } from 'react'
import { api, gbpExact } from '../api'
import type { CustomAsset, Investment, LedgerAccountType, LedgerEntry, SavingsAccount } from '../types'

const ACCOUNT_TYPES: { value: LedgerAccountType; label: string }[] = [
  { value: 'Savings', label: 'Savings account' },
  { value: 'Investment', label: 'Investment' },
  { value: 'CustomAsset', label: 'Other asset' },
]

const today = () => new Date().toISOString().slice(0, 10)

export default function LedgerPage() {
  const [entries, setEntries] = useState<LedgerEntry[]>([])
  const [savingsAccounts, setSavingsAccounts] = useState<SavingsAccount[]>([])
  const [investments, setInvestments] = useState<Investment[]>([])
  const [customAssets, setCustomAssets] = useState<CustomAsset[]>([])
  const [date, setDate] = useState(today())
  const [description, setDescription] = useState('')
  const [accountType, setAccountType] = useState<LedgerAccountType>('Savings')
  const [accountId, setAccountId] = useState('')
  const [amount, setAmount] = useState('')
  const [error, setError] = useState<string | null>(null)

  const load = () => {
    Promise.all([
      api.get<LedgerEntry[]>('/api/ledger'),
      api.get<SavingsAccount[]>('/api/savings'),
      api.get<Investment[]>('/api/investments'),
      api.get<CustomAsset[]>('/api/custom-assets'),
    ])
      .then(([e, s, i, a]) => {
        setEntries(e); setSavingsAccounts(s); setInvestments(i); setCustomAssets(a)
      })
      .catch((err) => setError(err.message))
  }
  useEffect(load, [])

  const options = accountType === 'Savings' ? savingsAccounts
    : accountType === 'Investment' ? investments
    : customAssets

  const accountName = (type: LedgerAccountType, id: string) => {
    const list = type === 'Savings' ? savingsAccounts : type === 'Investment' ? investments : customAssets
    return list.find((o) => o.id === id)?.name ?? 'Deleted account'
  }

  async function add(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    try {
      await api.post('/api/ledger', {
        date,
        description,
        amount: Number(amount),
        accountType,
        accountId,
      })
      setDescription(''); setAmount(''); setAccountId(''); setDate(today())
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add transaction.')
    }
  }

  async function remove(id: string) {
    await api.del(`/api/ledger/${id}`)
    load()
  }

  return (
    <>
      <h1>Ledger</h1>
      <p className="lede">
        One-off cash injections and payouts — a bonus deposited, an inheritance, money pulled out
        for a big purchase. Applied immediately to the account's balance, and kept here as a record
        of what happened and why.
      </p>
      {error && <div className="error-box">{error}</div>}

      <div className="card">
        {entries.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Description</th>
                <th>Account</th>
                <th className="num">Amount</th>
                <th className="num"></th>
              </tr>
            </thead>
            <tbody>
              {entries.map((entry) => (
                <tr key={entry.id}>
                  <td>{entry.date}</td>
                  <td>{entry.description}</td>
                  <td>{accountName(entry.accountType, entry.accountId)}</td>
                  <td className={`num value ${entry.amount >= 0 ? 'positive' : 'negative'}`}>
                    {entry.amount >= 0 ? '+' : ''}{gbpExact.format(entry.amount)}
                  </td>
                  <td className="num">
                    <button className="danger" onClick={() => remove(entry.id)}>Remove</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <form className="grid" onSubmit={add} style={{ marginTop: entries.length ? '1rem' : 0 }}>
          <div className="field">
            <label>Date</label>
            <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
          </div>
          <div className="field">
            <label>Description</label>
            <input value={description} onChange={(e) => setDescription(e.target.value)}
              placeholder="e.g. Annual bonus" required />
          </div>
          <div className="field">
            <label>Account type</label>
            <select value={accountType}
              onChange={(e) => { setAccountType(e.target.value as LedgerAccountType); setAccountId('') }}>
              {ACCOUNT_TYPES.map((t) => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Account</label>
            <select value={accountId} onChange={(e) => setAccountId(e.target.value)} required>
              <option value="">Select one…</option>
              {options.map((o) => (
                <option key={o.id} value={o.id}>{o.name}</option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Amount (£)</label>
            <input type="number" step="0.01" value={amount}
              onChange={(e) => setAmount(e.target.value)} required />
            <span className="muted">Positive for a cash injection, negative for a payout.</span>
          </div>
          <button type="submit">Add transaction</button>
        </form>
      </div>
    </>
  )
}
