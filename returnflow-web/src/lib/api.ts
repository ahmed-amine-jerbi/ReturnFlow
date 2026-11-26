import type {
  AppConfig,
  AuthResponse,
  Claim,
  Outcome,
  Processing,
  Range,
  ReturnRequest,
  StaffClaim,
  Stats,
} from './types'

const TOKEN_KEY = 'rf_token'

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (t: string) => localStorage.setItem(TOKEN_KEY, t),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body) headers.set('Content-Type', 'application/json')
  const token = tokenStore.get()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let res: Response
  try {
    res = await fetch(`/api${path}`, { ...init, headers })
  } catch {
    throw new ApiError('Cannot reach the server. Check your connection.', 0)
  }

  if (res.status === 401 && token) window.dispatchEvent(new Event('rf-unauthorized'))
  if (!res.ok) throw new ApiError(await readError(res), res.status)
  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

async function readError(res: Response): Promise<string> {
  try {
    const body = await res.json()
    if (typeof body?.error === 'string') return body.error
    if (body?.errors)
      return Object.values(body.errors as Record<string, string[]>)
        .flat()
        .join(' ')
    if (typeof body?.title === 'string') return body.title
  } catch {
    /* not JSON */
  }
  if (res.status === 401) return 'Invalid email or password.'
  if (res.status === 403) return 'You do not have access to this.'
  return `Request failed (${res.status}).`
}

const post = <T>(path: string, body?: unknown) =>
  request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) })

export const api = {
  config: () => request<AppConfig>('/config'),
  login: (email: string, password: string) => post<AuthResponse>('/auth/login', { email, password }),
  register: (email: string, password: string) => post<AuthResponse>('/auth/register', { email, password }),

  listReturns: () => request<ReturnRequest[]>('/returns'),
  createReturn: (body: { orderReference: string; purchaseDate: string; reason: string }) =>
    post<ReturnRequest>('/returns', body),
  addClaim: (returnId: number, body: { description: string; amountRequested: number }) =>
    post<Claim>(`/returns/${returnId}/claims`, body),
  requestReview: (claimId: number) => post<Claim>(`/claims/${claimId}/request-review`),

  staffClaims: (params: { processing?: Processing; status?: string; take?: number } = {}) => {
    const q = new URLSearchParams()
    if (params.processing) q.set('processing', params.processing)
    if (params.status) q.set('status', params.status)
    q.set('take', String(params.take ?? 100))
    return request<StaffClaim[]>(`/claims?${q}`)
  },
  decide: (claimId: number, outcome: Outcome, comment: string) =>
    request<Claim>(`/claims/${claimId}/decision`, {
      method: 'PATCH',
      body: JSON.stringify({ outcome, comment: comment.trim() || null }),
    }),

  stats: (range: Range) => request<Stats>(`/stats?range=${range}`),
}
