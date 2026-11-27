import { useI18n } from '../lib/i18n'

export function RiskMeter({ risk }: { risk: number }) {
  const { t } = useI18n()
  const pct = Math.round(Math.min(Math.max(risk, 0), 1) * 100)
  const level =
    risk < 0.3
      ? { text: t('risk.low'), color: 'text-emerald-700' }
      : risk < 0.6
        ? { text: t('risk.medium'), color: 'text-amber-700' }
        : { text: t('risk.high'), color: 'text-rose-700' }
  return (
    <div>
      <div className="mb-1.5 flex items-baseline justify-between">
        <span className={`text-sm font-semibold ${level.color}`}>{level.text}</span>
        <span className="text-sm tabular-nums text-slate-500">{pct} / 100</span>
      </div>
      <div className="relative h-2.5 rounded-full bg-gradient-to-r from-emerald-400 via-amber-400 to-rose-500">
        <span
          className="absolute top-1/2 size-4 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-white bg-slate-900 shadow"
          style={{ left: `${pct}%` }}
        />
      </div>
    </div>
  )
}
