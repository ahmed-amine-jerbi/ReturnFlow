import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { AiInsight } from '../components/AiInsight'
import { ClaimTimeline } from '../components/ClaimTimeline'
import { Badge, Button, Card, EmptyState, ErrorNote, Field, inputClass, Loading, Sheet } from '../components/ui'
import { api } from '../lib/api'
import { describeClaim, fmtDate, fmtMoney, prettyCategory, timeAgo } from '../lib/format'
import { useI18n } from '../lib/i18n'
import type { Outcome, StaffClaim } from '../lib/types'

type Tab = 'queue' | 'done'

export default function ReviewQueue() {
  const { t } = useI18n()
  const [tab, setTab] = useState<Tab>('queue')
  const [selectedId, setSelectedId] = useState<number | null>(null)

  const open = useQuery({
    queryKey: ['staff', 'open'],
    queryFn: () => api.staffClaims({ status: 'Open', take: 200 }),
    refetchInterval: 5000,
  })
  const done = useQuery({
    queryKey: ['staff', 'done'],
    queryFn: () => api.staffClaims({ take: 60 }),
    enabled: tab === 'done',
  })

  const waiting = (open.data ?? [])
    .filter((x) => x.claim.processing === 'NeedsHuman' || x.claim.processing === 'HumanReviewRequested')
    .sort((a, b) => +new Date(a.claim.createdAt) - +new Date(b.claim.createdAt)) // oldest first
  const finished = (done.data ?? []).filter((x) => x.claim.status !== 'Open')
  const list = tab === 'queue' ? waiting : finished
  const loading = tab === 'queue' ? open.isLoading : done.isLoading
  const selected = [...waiting, ...finished].find((x) => x.claim.id === selectedId) ?? null

  return (
    <div className="mx-auto w-full max-w-4xl px-4 py-6 sm:py-8">
      <h1 className="text-2xl font-bold tracking-tight text-slate-900">{t('queue.title')}</h1>
      <p className="mt-1 text-sm text-slate-500">{t('queue.description')}</p>

      <div className="mt-5 inline-flex rounded-xl bg-slate-200/70 p-1">
        {(
          [
            ['queue', t('queue.needsReview', { count: waiting.length })],
            ['done', t('queue.recentlyDecided')],
          ] as const
        ).map(([key, label]) => (
          <button
            key={key}
            onClick={() => setTab(key)}
            className={`min-h-10 rounded-lg px-4 text-sm font-semibold transition ${tab === key ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'}`}
          >
            {label}
          </button>
        ))}
      </div>

      <div className="mt-5 space-y-3">
        {loading && <Loading />}
        {!loading && list.length === 0 && (
          <EmptyState
            title={tab === 'queue' ? t('queue.emptyTitle') : t('queue.noDecisionsTitle')}
            text={tab === 'queue' ? t('queue.emptyText') : t('queue.noDecisionsText')}
          />
        )}
        {list.map((x) => (
          <QueueRow key={x.claim.id} item={x} onOpen={() => setSelectedId(x.claim.id)} />
        ))}
      </div>

      {selected && <DecideSheet item={selected} onClose={() => setSelectedId(null)} />}
    </div>
  )
}

function QueueRow({ item, onOpen }: { item: StaffClaim; onOpen: () => void }) {
  const { t, language } = useI18n()
  const { claim } = item
  const s = describeClaim(claim, t)
  const risk = claim.aiRiskScore
  const riskTone =
    risk == null ? 'bg-slate-300' : risk < 0.3 ? 'bg-emerald-500' : risk < 0.6 ? 'bg-amber-400' : 'bg-rose-500'
  return (
    <button onClick={onOpen} className="block w-full text-left">
      <Card className="rf-fade-up flex items-center gap-4 px-5 py-4 transition hover:border-indigo-300 hover:shadow-md">
        <span className={`h-12 w-1.5 shrink-0 rounded-full ${riskTone}`} title={t('ai.riskLabel')} />
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
            <p className="text-sm font-semibold text-slate-900">{item.orderReference}</p>
            <span className="text-xs text-slate-400">{timeAgo(claim.createdAt, t)}</span>
            {claim.humanReviewRequested && <Badge tone="amber">{t('queue.customerAsked')}</Badge>}
          </div>
          <p className="mt-0.5 truncate text-sm text-slate-600">{claim.description}</p>
          <p className="mt-0.5 truncate text-xs text-slate-400">
            {item.customerEmail} · {prettyCategory(claim.aiCategory, t)}
          </p>
        </div>
        <div className="shrink-0 text-right">
          <p className="text-base font-bold tabular-nums text-slate-900">{fmtMoney(claim.amountRequested, language)}</p>
          <div className="mt-1">
            <Badge tone={s.tone}>{s.label}</Badge>
          </div>
        </div>
      </Card>
    </button>
  )
}

function DecideSheet({ item, onClose }: { item: StaffClaim; onClose: () => void }) {
  const { t, language } = useI18n()
  const qc = useQueryClient()
  const { claim } = item
  const [comment, setComment] = useState('')
  const decide = useMutation({
    mutationFn: (outcome: Outcome) => api.decide(claim.id, outcome, comment),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['staff'] })
      onClose()
    },
  })

  return (
    <Sheet title={t('claim.sheetTitle', { id: claim.id, order: item.orderReference })} onClose={onClose}>
      <div className="space-y-5">
        <div className="grid grid-cols-2 gap-3 text-sm">
          <Info label={t('queue.customer')} value={item.customerEmail} />
          <Info label={t('queue.amount')} value={fmtMoney(claim.amountRequested, language)} />
          <Info label={t('queue.item')} value={item.reason} />
          <Info label={t('queue.purchased')} value={fmtDate(item.purchaseDate, language)} />
        </div>
        <p className="rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-700">{claim.description}</p>
        <AiInsight claim={claim} />

        {claim.status === 'Open' ? (
          <div className="space-y-3 border-t border-slate-100 pt-5">
            <Field label={t('queue.comment')}>
              <textarea
                className={`${inputClass} min-h-20`}
                maxLength={500}
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                placeholder={t('queue.commentPlaceholder')}
              />
            </Field>
            {decide.isError && <ErrorNote message={(decide.error as Error).message} />}
            <div className="flex gap-3">
              <Button
                variant="danger"
                className="flex-1"
                busy={decide.isPending && decide.variables === 'Rejected'}
                disabled={decide.isPending}
                onClick={() => decide.mutate('Rejected')}
              >
                {t('queue.reject')}
              </Button>
              <Button
                variant="success"
                className="flex-1"
                busy={decide.isPending && decide.variables === 'Approved'}
                disabled={decide.isPending}
                onClick={() => decide.mutate('Approved')}
              >
                {t('queue.approve')}
              </Button>
            </div>
          </div>
        ) : (
          <div className="border-t border-slate-100 pt-5">
            <h3 className="mb-3 text-sm font-semibold text-slate-900">{t('queue.history')}</h3>
            <ClaimTimeline claim={claim} />
          </div>
        )}
      </div>
    </Sheet>
  )
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="min-w-0">
      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</p>
      <p className="truncate font-medium text-slate-900">{value}</p>
    </div>
  )
}
