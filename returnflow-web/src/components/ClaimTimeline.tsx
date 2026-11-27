import { fmtDateTime, prettyCategory } from '../lib/format'
import { translateAnalysisText, useI18n } from '../lib/i18n'
import type { Claim } from '../lib/types'

interface Step {
  key: string
  title: string
  detail?: string
  at?: string
  tone: 'done' | 'pending' | 'bad' | 'info'
}

export function ClaimTimeline({ claim }: { claim: Claim }) {
  const { t, language } = useI18n()
  const steps: Step[] = [{ key: 'filed', title: t('timeline.filed'), at: claim.createdAt, tone: 'done' }]

  if (claim.analyzedAt) {
    steps.push({
      key: 'analysis',
      title: t('timeline.analysis'),
      detail: `${prettyCategory(claim.aiCategory, t)}, ${t('risk.label')} ${Math.round((claim.aiRiskScore ?? 0) * 100)}/100`,
      at: claim.analyzedAt,
      tone: 'info',
    })
  } else if (claim.processing === 'Analyzing') {
    steps.push({ key: 'analysis', title: t('timeline.analysisProgress'), tone: 'pending' })
  }

  claim.history.forEach((d, i) => {
    steps.push({
      key: `decision-${d.id}`,
      title: `${d.outcome === 'Approved' ? t('timeline.approved') : t('timeline.rejected')} ${d.decidedBy === 'Auto' ? t('timeline.automatically') : t('timeline.byAgent')}`,
      detail: d.comment ?? undefined,
      at: d.decidedAt,
      tone: d.outcome === 'Approved' ? 'done' : 'bad',
    })
    // An automatic decision that a person asked to have reviewed is followed by a review step.
    const next = claim.history[i + 1]
    if (d.decidedBy === 'Auto' && claim.humanReviewRequested && (next || claim.processing === 'HumanReviewRequested')) {
      steps.push(
        next
          ? { key: `review-${d.id}`, title: t('timeline.reviewRequested'), tone: 'info' }
          : { key: `review-${d.id}`, title: t('timeline.reviewWaiting'), tone: 'pending' },
      )
    }
  })

  if (claim.processing === 'NeedsHuman') {
    steps.push({
      key: 'waiting',
      title: t('timeline.waitingAgent'),
      detail: claim.routingReason ? translateAnalysisText(claim.routingReason, t) : undefined,
      tone: 'pending',
    })
  }

  const dot = {
    done: 'bg-emerald-500',
    bad: 'bg-rose-500',
    info: 'bg-indigo-500',
    pending: 'bg-amber-400 animate-pulse',
  }

  return (
    <ol className="relative ml-2 space-y-5 border-l border-slate-200 pl-6">
      {steps.map((s) => (
        <li key={s.key} className="relative">
          <span className={`absolute -left-[31px] top-1 size-3 rounded-full ring-4 ring-white ${dot[s.tone]}`} />
          <p className="text-sm font-semibold text-slate-900">{s.title}</p>
          {s.detail && <p className="mt-0.5 text-sm text-slate-600">{s.detail}</p>}
          {s.at && <p className="mt-0.5 text-xs text-slate-400">{fmtDateTime(s.at, language)}</p>}
        </li>
      ))}
    </ol>
  )
}
