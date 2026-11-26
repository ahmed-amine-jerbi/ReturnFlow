export type Role = 'Customer' | 'Agent' | 'Manager' | 'Admin'
export type ClaimStatus = 'Open' | 'Approved' | 'Rejected'
export type Processing = 'Analyzing' | 'AutoDecided' | 'NeedsHuman' | 'HumanReviewRequested' | 'HumanDecided'
export type Decider = 'Auto' | 'Human'
export type Outcome = 'Approved' | 'Rejected'
export type Range = 'd1' | 'd7' | 'lifetime'

export interface AuthResponse {
  token: string
  email: string
  role: Role
}

export interface AppConfig {
  demoMode: boolean
  demoPassword: string | null
  demoAccounts: { role: Role; email: string }[]
}

export interface Decision {
  id: number
  outcome: Outcome
  decidedBy: Decider
  comment: string | null
  decidedAt: string
}

export interface Claim {
  id: number
  returnRequestId: number
  description: string
  amountRequested: number
  status: ClaimStatus
  processing: Processing
  routingReason: string | null
  humanReviewRequested: boolean
  createdAt: string
  aiCategory: string | null
  aiRiskScore: number | null
  aiConfidence: number | null
  aiReasons: string[]
  aiModelVersion: string | null
  analyzedAt: string | null
  decision: Decision | null
  history: Decision[]
}

export interface ReturnRequest {
  id: number
  orderReference: string
  purchaseDate: string
  reason: string
  status: string
  createdAt: string
  claims: Claim[]
}

export interface StaffClaim {
  claim: Claim
  orderReference: string
  reason: string
  purchaseDate: string
  customerEmail: string
}

export interface StatsCore {
  returnsCreated: number
  claimsFiled: number
  aiAnalyzed: number
  claimsProcessed: number
  autoDecided: number
  humanDecided: number
  humanReviewRequests: number
  automationRatePercent: number
  approvalRatePercent: number
  approvedAmount: number
  avgMinutesAuto: number
  avgMinutesHuman: number
  avgRiskScore: number
}

export interface Stats {
  range: Range
  generatedAt: string
  from: string | null
  current: StatsCore
  previous: StatsCore | null
  backlog: { awaitingAi: number; needsHuman: number; reviewRequested: number }
  categories: { category: string; count: number }[]
  timeline: { label: string; filed: number; auto: number; human: number }[]
}
