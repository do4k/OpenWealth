export type StudentLoanPlan = 'Plan1' | 'Plan2' | 'Plan4' | 'Plan5' | 'Postgraduate'
export type PensionMethod = 'SalarySacrifice' | 'NetPay' | 'ReliefAtSource'
export type MortgageRateType = 'Fixed' | 'Variable'
export type ReinvestDestinationType = 'None' | 'Savings' | 'Investment'
export type LedgerAccountType = 'Savings' | 'Investment' | 'CustomAsset'
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
  expectedAnnualGrowthPercent: number | null
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
  reinvestDestinationType: ReinvestDestinationType
  reinvestDestinationId: string | null
  reinvestMonthlyAmount: number | null
  isPaidOff: boolean
}

export interface SavingsAccount {
  id: string
  name: string
  type: SavingsAccountType
  balance: number
  annualInterestRatePercent: number | null
  monthlyDeposit: number
}

export interface Investment {
  id: string
  name: string
  type: InvestmentType
  currentValue: number
  expectedAnnualGrowthPercent: number | null
  receivesIncomePensionContributions: boolean
}

export interface CustomAsset {
  id: string
  name: string
  value: number
  expectedAnnualGrowthPercent: number | null
}

export interface CustomDebt {
  id: string
  name: string
  balance: number
  annualInterestRatePercent: number | null
  monthlyPayment: number | null
  expectedAnnualGrowthPercent: number | null
  reinvestDestinationType: ReinvestDestinationType
  reinvestDestinationId: string | null
  reinvestMonthlyAmount: number | null
  isPaidOff: boolean
}

export interface IncomeDetails {
  annualSalary: number
  annualBonus: number
  employeePensionPercent: number
  employerPensionPercent: number
  pensionMethod: PensionMethod
  pensionOnBonus: boolean
  childrenReceivingChildBenefit: number
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
  childcareIncomeLimit: number
  hicbcLowerThreshold: number
  hicbcUpperThreshold: number
  childBenefitFirstChildWeekly: number
  childBenefitAdditionalChildWeekly: number
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

export interface FamilyBenefits {
  adjustedNetIncome: number
  childcareIncomeLimit: number
  losesFreeChildcare: boolean
  childcareHeadroom: number
  childrenReceivingChildBenefit: number
  annualChildBenefit: number
  hicbcPercent: number
  hicbcCharge: number
  netChildBenefit: number
}

export interface TakeHomeBreakdown {
  grossIncome: number
  employeePensionContribution: number
  employerPensionContribution: number
  adjustedNetIncome: number
  personalAllowance: number
  taxableIncome: number
  incomeTax: number
  nationalInsurance: number
  studentLoanRepayments: { plan: StudentLoanPlan; annualRepayment: number }[]
  totalStudentLoanRepayments: number
  annualTakeHome: number
  monthlyTakeHome: number
  familyBenefits: FamilyBenefits
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

export interface AutomationSettings {
  enabled: boolean
  paydayDayOfMonth: number
  lastAccrualDate: string | null
}

export interface WealthPoint {
  date: string
  netWorth: number
  totalAssets: number
  totalLiabilities: number
  property: number
  savings: number
  investments: number
  otherAssets: number
  mortgages: number
  studentLoans: number
  otherDebts: number
}

export interface AccrualEvent {
  date: string
  category: string
  itemName: string
  interestAmount: number
  paymentAmount: number
  depositAmount: number
  newBalance: number
}

export interface HistoryResponse {
  snapshots: WealthPoint[]
  events: AccrualEvent[]
}

export interface LedgerEntry {
  id: string
  date: string
  description: string
  amount: number
  accountType: LedgerAccountType
  accountId: string
}

export interface PublicTrendPoint {
  date: string
  netWorth: number
  totalAssets?: number
  totalLiabilities?: number
  property?: number
  savings?: number
  investments?: number
  otherAssets?: number
  mortgages?: number
  studentLoans?: number
  otherDebts?: number
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
  history: PublicTrendPoint[]
  projection: PublicTrendPoint[]
}
