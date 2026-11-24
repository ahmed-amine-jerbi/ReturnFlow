import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, Cell, ComposedChart, Legend, Line, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { api } from '../lib/api'
import type { Range, Stats } from '../lib/types'
import { fmtMinutes, fmtMoney, fmtPercent, prettyCategory } from '../lib/format'
import { Card, ErrorNote, Loading } from '../components/ui'
import { useI18n } from '../lib/i18n'

const RANGES: { key: Range; label: string; longKey: string }[] = [
  { key: 'd1', label: 'D1', longKey: 'dashboard.last24' },
  { key: 'd7', label: 'D7', longKey: 'dashboard.last7' },
  { key: 'lifetime', label: '', longKey: 'dashboard.allTime' },
]

const AUTO = '#4f46e5'
const HUMAN = '#f59e0b'
const FILED = '#0ea5e9'

export default function Dashboard() {
  const { t } = useI18n()
  const [range, setRange] = useState<Range>('d7')
  const stats = useQuery({
    queryKey: ['stats', range],
    queryFn: () => api.stats(range),
    refetchInterval: 10000,
    placeholderData: (prev) => prev,
  })

  return (
    <div className="mx-auto w-full max-w-6xl px-4 py-6 sm:py-8">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">{t('dashboard.title')}</h1>
          <p className="mt-1 text-sm text-slate-500">{t('dashboard.description')}</p>
        </div>
        <div className="inline-flex rounded-xl bg-slate-200/70 p-1" role="tablist" aria-label={t('dashboard.timeRange')}>
          {RANGES.map((r) => (
            <button key={r.key} role="tab" aria-selected={range === r.key} onClick={() => setRange(r.key)} className={`min-h-10 min-w-16 rounded-lg px-4 text-sm font-semibold transition ${range === r.key ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'}`}>
              {r.key === 'lifetime' ? t('dashboard.rangeLifetime') : r.label}
            </button>
          ))}
        </div>
      </div>

      {stats.isLoading && <Loading label={t('dashboard.calculating')} />}
      {stats.isError && <div className="mt-6"><ErrorNote message={(stats.error as Error).message} /></div>}
      {stats.data && <Content s={stats.data} />}
    </div>
  )
}

function Content({ s }: { s: Stats }) {
  const { t, language } = useI18n()
  const c = s.current
  const p = s.previous
  const share = [
    { name: t('dashboard.automatic'), value: c.autoDecided, color: AUTO },
    { name: t('dashboard.agents'), value: c.humanDecided, color: HUMAN },
  ]
  const totalShare = c.autoDecided + c.humanDecided
  const waiting = s.backlog.needsHuman + s.backlog.reviewRequested

  return (
    <div className="mt-6 space-y-6">
      <Recap s={s} />

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <Stat label={t('dashboard.processed')} value={String(c.claimsProcessed)} delta={pct(c.claimsProcessed, p?.claimsProcessed)} good="up" />
        <Stat label={t('dashboard.automationRate')} value={fmtPercent(c.automationRatePercent, language)} hint={t('dashboard.noHuman')} delta={pts(c.automationRatePercent, p?.automationRatePercent)} good="up" />
        <Stat label={t('dashboard.decidedAi')} value={String(c.autoDecided)} delta={pct(c.autoDecided, p?.autoDecided)} good="up" />
        <Stat label={t('dashboard.decidedAgents')} value={String(c.humanDecided)} delta={pct(c.humanDecided, p?.humanDecided)} good="none" />
        <Stat label={t('dashboard.reviewsRequested')} value={String(c.humanReviewRequests)} hint={t('dashboard.askedBy')} delta={pct(c.humanReviewRequests, p?.humanReviewRequests)} good="none" />
        <Stat label={t('dashboard.approvalRate')} value={fmtPercent(c.approvalRatePercent, language)} delta={pts(c.approvalRatePercent, p?.approvalRatePercent)} good="none" />
        <Stat label={t('dashboard.timeDecision')} value={fmtMinutes(c.avgMinutesAuto, t, language)} hint={`${t('dashboard.agents')}: ${fmtMinutes(c.avgMinutesHuman, t, language)}`} />
        <Stat label={t('dashboard.approvedAmount')} value={fmtMoney(c.approvedAmount, language)} delta={pct(c.approvedAmount, p?.approvedAmount)} good="none" />
      </div>

      <div className="grid gap-4 lg:grid-cols-5">
        <Card className="p-5 lg:col-span-2">
          <h2 className="text-sm font-semibold text-slate-900">{t('dashboard.involvement')}</h2>
          <p className="text-xs text-slate-500">{t('dashboard.involvementDescription')}</p>
          <div className="relative mt-2 h-56">
            {totalShare === 0 ? (
              <p className="grid h-full place-items-center text-sm text-slate-400">{t('dashboard.noDecisions')}</p>
            ) : (
              <>
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie data={share} dataKey="value" nameKey="name" innerRadius="62%" outerRadius="88%" paddingAngle={2} stroke="none">
                      {share.map((d) => <Cell key={d.name} fill={d.color} />)}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
                <div className="pointer-events-none absolute inset-0 grid place-items-center text-center">
                  <div>
                    <p className="text-3xl font-bold tabular-nums text-slate-900">{fmtPercent(c.automationRatePercent, language)}</p>
                    <p className="text-xs text-slate-500">{t('dashboard.automated')}</p>
                  </div>
                </div>
              </>
            )}
          </div>
          <div className="mt-2 flex flex-wrap justify-center gap-x-5 gap-y-1 text-xs text-slate-600">
            {share.map((d) => (
              <span key={d.name} className="inline-flex items-center gap-1.5"><span className="size-2.5 rounded-full" style={{ background: d.color }} />{d.name}: <b className="tabular-nums">{d.value}</b></span>
            ))}
          </div>
        </Card>

        <Card className="p-5 lg:col-span-3">
          <h2 className="text-sm font-semibold text-slate-900">{t('dashboard.activity')}</h2>
          <p className="text-xs text-slate-500">{t(s.range === 'd1' ? 'dashboard.hourly' : s.range === 'd7' ? 'dashboard.daily7' : 'dashboard.daily30')}</p>
          <div className="mt-3 h-64">
            <ResponsiveContainer width="100%" height="100%">
              <ComposedChart data={s.timeline} margin={{ top: 4, right: 8, left: -18, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
                <XAxis dataKey="label" tick={{ fontSize: 11, fill: '#64748b' }} tickLine={false} axisLine={false} interval="preserveStartEnd" minTickGap={18} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11, fill: '#64748b' }} tickLine={false} axisLine={false} />
                <Tooltip />
                <Legend wrapperStyle={{ fontSize: 12 }} />
                <Bar dataKey="auto" name={t('dashboard.chartAutomatic')} stackId="d" fill={AUTO} radius={[0, 0, 0, 0]} />
                <Bar dataKey="human" name={t('dashboard.chartAgents')} stackId="d" fill={HUMAN} radius={[4, 4, 0, 0]} />
                <Line dataKey="filed" name={t('dashboard.claimsFiled')} type="monotone" stroke={FILED} strokeWidth={2} dot={false} />
              </ComposedChart>
            </ResponsiveContainer>
          </div>
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-5">
        <Card className="p-5 lg:col-span-3">
          <h2 className="text-sm font-semibold text-slate-900">{t('dashboard.aboutClaims')}</h2>
          <p className="text-xs text-slate-500">{t('dashboard.categoriesFound')}</p>
          <div className="mt-3 h-60">
            {s.categories.length === 0 ? (
              <p className="grid h-full place-items-center text-sm text-slate-400">{t('dashboard.noClaims')}</p>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={s.categories.map((x) => ({ ...x, name: prettyCategory(x.category, t) }))} layout="vertical" margin={{ top: 0, right: 12, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="#e2e8f0" />
                  <XAxis type="number" allowDecimals={false} tick={{ fontSize: 11, fill: '#64748b' }} tickLine={false} axisLine={false} />
                  <YAxis type="category" dataKey="name" width={120} tick={{ fontSize: 12, fill: '#334155' }} tickLine={false} axisLine={false} />
                  <Tooltip />
                  <Bar dataKey="count" name={t('dashboard.claims')} fill={FILED} radius={[0, 6, 6, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </Card>

        <Card className="p-5 lg:col-span-2">
          <h2 className="text-sm font-semibold text-slate-900">{t('dashboard.rightNow')}</h2>
          <p className="text-xs text-slate-500">{t('dashboard.liveBacklog')}</p>
          <dl className="mt-4 space-y-3">
            <Backlog label={t('dashboard.waitingAgent')} value={s.backlog.needsHuman} tone={s.backlog.needsHuman > 0 ? 'text-amber-600' : 'text-slate-900'} />
            <Backlog label={t('dashboard.reviewRequested')} value={s.backlog.reviewRequested} tone={s.backlog.reviewRequested > 0 ? 'text-amber-600' : 'text-slate-900'} />
            <Backlog label={t('dashboard.aiAnalyzing')} value={s.backlog.awaitingAi} tone="text-indigo-600" />
          </dl>
          <p className={`mt-5 rounded-xl px-4 py-3 text-sm ${waiting === 0 ? 'bg-emerald-50 text-emerald-800' : 'bg-amber-50 text-amber-900'}`}>
            {waiting === 0 ? t('dashboard.noWaiting') : t('dashboard.waitingCount', { count: waiting })}
          </p>
          <p className="mt-4 text-xs text-slate-400">{t('dashboard.avgRisk', { score: c.avgRiskScore.toFixed(2) })}</p>
        </Card>
      </div>
    </div>
  )
}

/* ---------- recap ---------- */

function Recap({ s }: { s: Stats }) {
  const { t, language } = useI18n()
  const lines = buildRecap(s, t, language)
  const label = t(RANGES.find((r) => r.key === s.range)?.longKey ?? 'dashboard.allTime')
  return (
    <section className="relative overflow-hidden rounded-3xl bg-slate-900 p-6 text-white shadow-sm sm:p-8">
      <div className="absolute -right-16 -top-16 size-64 rounded-full bg-indigo-600/30 blur-3xl" />
      <div className="relative">
        <p className="text-xs font-semibold uppercase tracking-widest text-sky-300">{t('dashboard.recap', { range: label })}</p>
        <p className="mt-2 text-2xl font-bold leading-snug tracking-tight sm:text-3xl">{lines[0]}</p>
        <ul className="mt-4 space-y-2 text-sm text-slate-300 sm:text-base">
          {lines.slice(1).map((l) => (
            <li key={l} className="flex gap-3"><span className="mt-2 size-1.5 shrink-0 rounded-full bg-sky-400" />{l}</li>
          ))}
        </ul>
      </div>
    </section>
  )
}

function buildRecap(s: Stats, t: ReturnType<typeof useI18n>['t'], language: ReturnType<typeof useI18n>['language']): string[] {
  const c = s.current
  const label = t(RANGES.find((r) => r.key === s.range)?.longKey ?? 'dashboard.allTime')
  const waiting = s.backlog.needsHuman + s.backlog.reviewRequested
  const backlog = waiting === 0 ? t('dashboard.noWaiting') : t('dashboard.waitingRecap', { count: waiting })

  if (c.claimsProcessed === 0) return [t('dashboard.noneDecided', { range: label }), backlog]

  const out = [t('dashboard.decided', { count: c.claimsProcessed, range: label })]
  const change = s.previous ? pct(c.claimsProcessed, s.previous.claimsProcessed) : null
  if (change && change.kind === 'pct') {
    out.push(t(change.value >= 0 ? 'dashboard.changeUp' : 'dashboard.changeDown', { change: Math.abs(change.value) }))
  }
  out.push(t('dashboard.automationRecap', {
    percent: fmtPercent(c.automationRatePercent, language),
    auto: c.autoDecided,
    human: c.humanDecided,
  }))
  if (c.humanReviewRequests > 0) out.push(t('dashboard.reviewsRecap', { count: c.humanReviewRequests }))
  if (c.avgMinutesAuto && c.avgMinutesHuman) {
    out.push(t('dashboard.timeRecap', {
      auto: fmtMinutes(c.avgMinutesAuto, t, language),
      human: fmtMinutes(c.avgMinutesHuman, t, language),
    }))
  }
  out.push(t('dashboard.approvalsRecap', {
    percent: fmtPercent(c.approvalRatePercent, language),
    amount: fmtMoney(c.approvedAmount, language),
  }))
  const top = s.categories[0]
  if (top) out.push(t('dashboard.commonIssue', {
    category: prettyCategory(top.category, t).toLocaleLowerCase(language),
    count: top.count,
  }))
  out.push(backlog)
  return out
}

/* ---------- small pieces ---------- */

interface Delta { kind: 'pct' | 'pts'; value: number }

function pct(cur: number, prev: number | undefined | null): Delta | null {
  if (prev == null || prev === 0) return null
  return { kind: 'pct', value: Math.round(((cur - prev) / prev) * 100) }
}

function pts(cur: number, prev: number | undefined | null): Delta | null {
  if (prev == null) return null
  return { kind: 'pts', value: Math.round((cur - prev) * 10) / 10 }
}

function Stat({ label, value, hint, delta, good = 'none' }: { label: string; value: string; hint?: string; delta?: Delta | null; good?: 'up' | 'down' | 'none' }) {
  const { t } = useI18n()
  let deltaEl = null
  if (delta && delta.value !== 0) {
    const up = delta.value > 0
    const positive = good === 'none' ? null : good === 'up' ? up : !up
    const color = positive === null ? 'text-slate-500' : positive ? 'text-emerald-600' : 'text-rose-600'
    deltaEl = (
      <span className={`text-xs font-semibold tabular-nums ${color}`}>
        {up ? '▲' : '▼'} {Math.abs(delta.value)}{delta.kind === 'pct' ? '%' : t('delta.points')}
      </span>
    )
  }
  return (
    <Card className="p-4 sm:p-5">
      <p className="text-xs font-medium text-slate-500">{label}</p>
      <div className="mt-1.5 flex flex-wrap items-baseline gap-x-2">
        <p className="text-2xl font-bold tabular-nums tracking-tight text-slate-900 sm:text-3xl">{value}</p>
        {deltaEl}
      </div>
      {hint && <p className="mt-1 text-xs text-slate-400">{hint}</p>}
    </Card>
  )
}

function Backlog({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <div className="flex items-center justify-between">
      <dt className="text-sm text-slate-600">{label}</dt>
      <dd className={`text-2xl font-bold tabular-nums ${tone}`}>{value}</dd>
    </div>
  )
}
