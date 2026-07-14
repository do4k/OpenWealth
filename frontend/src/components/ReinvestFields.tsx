import { gbpExact } from '../api'
import type { Investment, ReinvestDestinationType, SavingsAccount } from '../types'

/**
 * The "once paid off, redirect the payment into..." controls shared by
 * mortgages and custom debts, in both their add-forms and inline-edit rows.
 * Kept string-typed so it works unmodified inside a controlled add-form
 * (which already stores every field as a string) and an inline-edit row
 * (which converts to/from the entity's typed fields at the call site).
 */
export function ReinvestFields({
  type,
  destinationId,
  amount,
  onTypeChange,
  onDestinationChange,
  onAmountChange,
  savingsAccounts,
  investments,
  suggestedAmount,
  compact = false,
}: {
  type: ReinvestDestinationType
  destinationId: string
  amount: string
  onTypeChange: (type: ReinvestDestinationType) => void
  onDestinationChange: (id: string) => void
  onAmountChange: (value: string) => void
  savingsAccounts: SavingsAccount[]
  investments: Investment[]
  suggestedAmount?: number
  compact?: boolean
}) {
  const options = type === 'Savings' ? savingsAccounts : type === 'Investment' ? investments : []
  const wrap = (label: string, control: React.ReactNode) =>
    compact ? control : (
      <div className="field">
        <label>{label}</label>
        {control}
      </div>
    )

  return (
    <>
      {wrap('Once paid off, reinvest into', (
        <select value={type} onChange={(e) => onTypeChange(e.target.value as ReinvestDestinationType)}>
          <option value="None">Don't reinvest</option>
          <option value="Savings">A savings account</option>
          <option value="Investment">An investment</option>
        </select>
      ))}
      {type !== 'None' && (
        <>
          {wrap('Destination', (
            <select value={destinationId} onChange={(e) => onDestinationChange(e.target.value)}>
              <option value="">Select one…</option>
              {options.map((o) => (
                <option key={o.id} value={o.id}>{o.name}</option>
              ))}
            </select>
          ))}
          {wrap('Monthly amount to reinvest (£)', (
            <input type="number" min="0" step="0.01" value={amount}
              onChange={(e) => onAmountChange(e.target.value)}
              placeholder={suggestedAmount ? suggestedAmount.toFixed(2) : undefined} />
          ))}
          {!compact && suggestedAmount != null && (
            <p className="muted" style={{ flexBasis: '100%', margin: 0 }}>
              Pre-filled with what you're currently paying ({gbpExact.format(suggestedAmount)}/month) — change
              it if you'd rather reinvest a different amount.
            </p>
          )}
        </>
      )}
    </>
  )
}
