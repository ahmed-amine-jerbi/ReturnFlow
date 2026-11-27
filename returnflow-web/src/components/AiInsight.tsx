import { prettyCategory } from '../lib/format'
import { translateAnalysisText, useI18n } from '../lib/i18n'
import type { Claim } from '../lib/types'
import { RiskMeter } from './RiskMeter'

/** What the AI saw and why a claim went where it did. Used for customers (read only) and agents. */
export function AiInsight({ claim }: { claim: Claim }) {
  const { t } = useI18n()
  if (claim.aiRiskScore == null) {
    return <p className="rounded-xl bg-slate-50 px-4 py-3 text-sm text-slate-500">{t('ai.unavailable')}</p>
  }
  return (
    <div className="space-y-4 rounded-2xl bg-slate-50 p-4 ring-1 ring-inset ring-slate-200">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-slate-500">{t('ai.category')}</p>
          <p className="text-sm font-semibold text-slate-900">{prettyCategory(claim.aiCategory, t)}</p>
        </div>
        <div className="text-right">
          <p className="text-xs font-medium uppercase tracking-wide text-slate-500">{t('ai.confidence')}</p>
          <p className="text-sm font-semibold tabular-nums text-slate-900">
            {Math.round((claim.aiConfidence ?? 0) * 100)}%
          </p>
        </div>
      </div>
      <RiskMeter risk={claim.aiRiskScore} />
      {claim.aiReasons.length > 0 && (
        <ul className="space-y-1.5">
          {claim.aiReasons.map((r) => (
            <li key={r} className="flex gap-2 text-sm text-slate-700">
              <span className="mt-1.5 size-1.5 shrink-0 rounded-full bg-slate-400" />
              {translateAnalysisText(r, t)}
            </li>
          ))}
        </ul>
      )}
      {claim.routingReason && (
        <p className="border-t border-slate-200 pt-3 text-xs text-slate-500">
          <span className="font-medium text-slate-600">{t('ai.routing')}</span>{' '}
          {translateAnalysisText(claim.routingReason, t)}
        </p>
      )}
    </div>
  )
}
