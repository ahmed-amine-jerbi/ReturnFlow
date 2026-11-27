import type { Language, Translator } from './i18n'
import type { Claim } from './types'

export const CURRENCY = 'USD'

export const fmtMoney = (n: number, locale: Language = 'en') =>
  new Intl.NumberFormat(locale, { style: 'currency', currency: CURRENCY, maximumFractionDigits: 2 }).format(n)

export const fmtPercent = (n: number, locale: Language = 'en') =>
  `${new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(n)}%`

export function fmtMinutes(min: number, t: Translator, locale: Language): string {
  if (!min) return '-'
  if (min < 1) return `${Math.max(1, Math.round(min * 60))} ${t('unit.seconds')}`
  if (min < 90) return `${Math.round(min)} ${t('unit.minutes')}`
  if (min < 60 * 48)
    return `${new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(min / 60)} ${t('unit.hours')}`
  return `${new Intl.NumberFormat(locale, { maximumFractionDigits: 1 }).format(min / 1440)} ${t('unit.days')}`
}

export function timeAgo(iso: string, t: Translator): string {
  const seconds = Math.round((Date.now() - new Date(iso).getTime()) / 1000)
  if (seconds < 45) return t('time.justNow')
  if (seconds < 3600) return t('time.minutesAgo', { count: Math.round(seconds / 60) })
  if (seconds < 86400) return t('time.hoursAgo', { count: Math.round(seconds / 3600) })
  return t('time.daysAgo', { count: Math.round(seconds / 86400) })
}

export const fmtDateTime = (iso: string, locale: Language) =>
  new Date(iso).toLocaleString(locale, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })

export const fmtDate = (iso: string, locale: Language) =>
  new Date(iso).toLocaleDateString(locale, { day: 'numeric', month: 'short', year: 'numeric' })

export const todayIso = () => new Date().toISOString().slice(0, 10)

export const prettyCategory = (c: string | null, t: Translator) => (c ? t(`category.${c}`) : t('category.unknown'))

export type Tone = 'slate' | 'indigo' | 'green' | 'red' | 'amber'

/** One friendly label for where a claim stands, used by every screen. */
export function describeClaim(c: Claim, t: Translator): { label: string; tone: Tone; pulse?: boolean } {
  switch (c.processing) {
    case 'Analyzing':
      return { label: t('status.analyzing'), tone: 'indigo', pulse: true }
    case 'NeedsHuman':
      return { label: t('status.needsHuman'), tone: 'amber' }
    case 'HumanReviewRequested':
      return { label: t('status.reviewRequested'), tone: 'amber' }
    case 'AutoDecided':
      return c.status === 'Approved'
        ? { label: t('status.approvedAuto'), tone: 'green' }
        : { label: t('status.rejectedAuto'), tone: 'red' }
    case 'HumanDecided':
      return c.status === 'Approved'
        ? { label: t('status.approvedHuman'), tone: 'green' }
        : { label: t('status.rejectedHuman'), tone: 'red' }
  }
}
