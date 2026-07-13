export type StudentLoanPlan = 'Plan1' | 'Plan2' | 'Plan4' | 'Plan5' | 'Postgraduate'
export type PensionMethod = 'SalarySacrifice' | 'NetPay' | 'ReliefAtSource'
export type MortgageRateType = 'Fixed' | 'Variable'
export type SavingsAccountType =
  | 'CurrentAccount' | 'EasyAccess' | 'FixedTerm' | 'CashIsa' | 'PremiumBonds' | 'Other'
export type InvestmentType =
  | 'StocksAndSharesIsa' | 'GeneralInvestmentAccount' | 'PensionPot' | 'LifetimeIsa' | 'Crypto' | 'Other'
export type ShareVisibility = 'NetWorthOnly' | 'CategoryTotals' | 'FullBreakdown'

export interface AuthResponse {
  token: string
  email: string
  displayName: string
}

export interface StudentLoan {
  id: string
  plan: StudentLoanPlan
  balance: number
  notes: string | null
}

export interface StudentLoanPlanSetting {
  id: string
  plan: StudentLoanPlan
  annualRepaymentThreshold: number
  repaymentRatePercent: number
  interestRatePercent: number
}

export interface Property {
  id: string
  name: string
  estimatedValue: number
}

export interface Mortgage {
  id: string
  name: string
  propertyId: string | null
  outstandingBalance: number
  annualInterestRatePercent: number
  rateType: MortgageRateType
  fixedRateEndDate: string | null
  followOnRatePercent: number | null
  termMonthsRemaining: number
  monthlyPayment: number
  isFixedPeriodOver: boolean
}

export interface SavingsAccount {
  id: string
  name: string
  type: SavingsAccountType
  balance: number
  annualInterestRatePercent: number | null
}

export interface Investment {
  id: string
  name: string
  type: InvestmentType
  currentValue: number
}

export interface IncomeDetails {
  annualSalary: number
  annualBonus: number
  employeePensionPercent: number
  employerPensionPercent: number
  pensionMethod: PensionMethod
  pensionOnBonus: boolean
}

export interface TaxSettings {
  taxYearLabel: string
  personalAllowance: number
  personalAllowanceTaperThreshold: number
  basicRateLimit: number
  higherRateLimit: number
  basicRatePercent: number
  higherRatePercent: number
  additionalRatePercent: number
  niPrimaryThresholdAnnual: number
  niUpperEarningsLimitAnnual: number
  niMainRatePercent: number
  niUpperRatePercent: number
}

export interface CategoryTotal {
  category: string
  total: number
}

export interface NetWorthItem {
  category: string
  name: string
  value: number
}

export interface TakeHomeBreakdown {
  grossIncome: number
  employeePensionContribution: number
  employerPensionContribution: number
  personalAllowance: number
  taxableIncome: number
  incomeTax: number
  nationalInsurance: number
  studentLoanRepayments: { plan: StudentLoanPlan; annualRepayment: number }[]
  totalStudentLoanRepayments: number
  annualTakeHome: number
  monthlyTakeHome: number
}

export interface WealthSummary {
  netWorth: number
  totalAssets: number
  totalLiabilities: number
  assetTotals: CategoryTotal[]
  liabilityTotals: CategoryTotal[]
  items: NetWorthItem[]
  takeHome: TakeHomeBreakdown | null
}

export interface ShareSettings {
  enabled: boolean
  slug: string | null
  visibility: ShareVisibility
  hasPassphrase: boolean
}

export interface PublicProfile {
  displayName: string
  visibility: ShareVisibility
  netWorth: number
  totalAssets?: number
  totalLiabilities?: number
  assetTotals?: CategoryTotal[]
  liabilityTotals?: CategoryTotal[]
  items?: NetWorthItem[]
}
