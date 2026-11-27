import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { AiInsight } from '../components/AiInsight'
import { ClaimTimeline } from '../components/ClaimTimeline'
import { Badge, Button, Card, EmptyState, ErrorNote, Loading, Sheet } from '../components/ui'
import { api } from '../lib/api'
import { describeClaim, fmtDate, fmtMoney, timeAgo } from '../lib/format'
import { useI18n } from '../lib/i18n'
import type { Claim, ReturnRequest } from '../lib/types'
import NewReturn from './NewReturn'

export default function MyReturns() {
  const { t } = useI18n()
  const [creating, setCreating] = useState(false)
  const [toast, setToast] = useState('')
  const [openClaimId, setOpenClaimId] = useState<number | null>(null)

  const returns = useQuery({
    queryKey: ['returns'],
    queryFn: api.listReturns,
    // While the AI is still working on a claim, check again every few seconds.
    refetchInterval: (q) =>
      q.state.data?.some((r) => r.claims.some((c) => c.processing === 'Analyzing')) ? 2500 : 30000,
  })

  const openClaim = returns.data?.flatMap((r) => r.claims).find((c) => c.id === openClaimId) ?? null

  return (
    <div className="mx-auto w-full max-w-3xl px-4 py-6 sm:py-8">
      <div className="flex items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">{t('returns.title')}</h1>
          <p className="mt-1 text-sm text-slate-500">{t('returns.description')}</p>
        </div>
        <Button onClick={() => setCreating(true)} className="shrink-0">
          {t('returns.new')}
        </Button>
      </div>

      {toast && (
        <div className="rf-fade-up mt-4 rounded-xl bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-800 ring-1 ring-inset ring-emerald-200">
          {toast}
        </div>
      )}

      <div className="mt-6 space-y-4">
        {returns.isLoading && <Loading />}
        {returns.isError && <ErrorNote message={(returns.error as Error).message} />}
        {returns.data?.length === 0 && (
          <EmptyState
            title={t('returns.emptyTitle')}
            text={t('returns.emptyText')}
            action={<Button onClick={() => setCreating(true)}>{t('returns.start')}</Button>}
          />
        )}
        {returns.data?.map((r) => (
          <ReturnCard key={r.id} item={r} onOpen={setOpenClaimId} />
        ))}
      </div>

      {creating && (
        <NewReturn
          onClose={() => setCreating(false)}
          onDone={() => {
            setCreating(false)
            setToast(t('returns.submitted'))
            window.setTimeout(() => setToast(''), 6000)
          }}
        />
      )}
      {openClaim && <ClaimSheet claim={openClaim} onClose={() => setOpenClaimId(null)} />}
    </div>
  )
}

function ReturnCard({ item, onOpen }: { item: ReturnRequest; onOpen: (claimId: number) => void }) {
  const { t, language } = useI18n()
  return (
    <Card className="rf-fade-up overflow-hidden">
      <div className="flex items-start justify-between gap-3 border-b border-slate-100 px-5 py-4">
        <div className="min-w-0">
          <p className="truncate text-base font-semibold text-slate-900">{item.reason}</p>
          <p className="mt-0.5 text-sm text-slate-500">
            {item.orderReference} · {t('returns.bought', { date: fmtDate(item.purchaseDate, language) })}
          </p>
        </div>
        <span className="shrink-0 text-xs text-slate-400">{timeAgo(item.createdAt, t)}</span>
      </div>
      {item.claims.length === 0 ? (
        <p className="px-5 py-4 text-sm text-slate-500">{t('returns.noClaim')}</p>
      ) : (
        <ul className="divide-y divide-slate-100">
          {item.claims.map((c) => {
            const s = describeClaim(c, t)
            return (
              <li key={c.id}>
                <button
                  type="button"
                  onClick={() => onOpen(c.id)}
                  className="flex w-full items-center justify-between gap-3 px-5 py-4 text-left transition hover:bg-slate-50"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm text-slate-700">{c.description}</p>
                    <p className="mt-1 text-sm font-semibold tabular-nums text-slate-900">
                      {fmtMoney(c.amountRequested, language)}
                    </p>
                  </div>
                  <Badge tone={s.tone} pulse={s.pulse}>
                    {s.label}
                  </Badge>
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </Card>
  )
}

function ClaimSheet({ claim, onClose }: { claim: Claim; onClose: () => void }) {
  const { t, language } = useI18n()
  const qc = useQueryClient()
  const s = describeClaim(claim, t)
  const review = useMutation({
    mutationFn: () => api.requestReview(claim.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['returns'] }),
  })

  return (
    <Sheet title={t('claim.title', { id: claim.id })} onClose={onClose}>
      <div className="space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <Badge tone={s.tone} pulse={s.pulse}>
            {s.label}
          </Badge>
          <span className="text-base font-bold tabular-nums">{fmtMoney(claim.amountRequested, language)}</span>
        </div>
        <p className="rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{claim.description}</p>

        {claim.processing === 'AutoDecided' && (
          <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4">
            <p className="text-sm font-semibold text-amber-900">{t('claim.notHappy')}</p>
            <p className="mt-1 text-sm text-amber-800">{t('claim.reviewExplain')}</p>
            {review.isError && (
              <div className="mt-3">
                <ErrorNote message={(review.error as Error).message} />
              </div>
            )}
            <Button variant="secondary" busy={review.isPending} onClick={() => review.mutate()} className="mt-3 w-full">
              {t('claim.askReview')}
            </Button>
          </div>
        )}

        <div>
          <h3 className="mb-3 text-sm font-semibold text-slate-900">{t('claim.progress')}</h3>
          <ClaimTimeline claim={claim} />
        </div>
        <div>
          <h3 className="mb-3 text-sm font-semibold text-slate-900">{t('claim.aiView')}</h3>
          <AiInsight claim={claim} />
        </div>
      </div>
    </Sheet>
  )
}
