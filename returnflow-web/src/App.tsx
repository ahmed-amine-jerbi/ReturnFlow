import { lazy, type ReactNode, Suspense, useEffect, useState } from 'react'
import { Badge, Button, Loading, Logo } from './components/ui'
import { canSeeKpis, isStaff, type User, useAuth } from './lib/auth'
import { LanguageSelect, useI18n } from './lib/i18n'
import Login from './pages/Login'
import MyReturns from './pages/MyReturns'
import ReviewQueue from './pages/ReviewQueue'

// The charts library is heavy, so the KPI page is only downloaded when a manager opens it.
const Dashboard = lazy(() => import('./pages/Dashboard'))

type TabKey = 'returns' | 'queue' | 'kpis'
interface Tab {
  key: TabKey
  label: string
  icon: ReactNode
}

const icon = (d: string) => (
  <svg
    viewBox="0 0 24 24"
    className="size-6"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.8"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <path d={d} />
  </svg>
)
const ICONS = {
  returns: icon('M21 8l-9-5-9 5v8l9 5 9-5V8zM3 8l9 5 9-5M12 13v8'),
  queue: icon('M4 13h4l2 3h4l2-3h4M4 13l2-8h12l2 8v6H4v-6z'),
  kpis: icon('M4 20V10M10 20V4M16 20v-7M22 20H2'),
}

export default function App() {
  const { user } = useAuth()
  return user ? <Shell user={user} /> : <Login />
}

function Shell({ user }: { user: User }) {
  const { signOut } = useAuth()
  const { t } = useI18n()
  const role = user.role

  const tabs: Tab[] = []
  if (canSeeKpis(role)) tabs.push({ key: 'kpis', label: t('nav.kpis'), icon: ICONS.kpis })
  if (isStaff(role)) tabs.push({ key: 'queue', label: t('nav.queue'), icon: ICONS.queue })
  if (role === 'Customer') tabs.push({ key: 'returns', label: t('nav.returns'), icon: ICONS.returns })

  const fromHash = window.location.hash.slice(1) as TabKey
  const [tab, setTab] = useState<TabKey>(tabs.some((t) => t.key === fromHash) ? fromHash : tabs[0].key)

  useEffect(() => {
    window.history.replaceState(null, '', `#${tab}`)
  }, [tab])

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-30 border-b border-slate-200 bg-white/90 backdrop-blur safe-top">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4">
          <div className="flex items-center gap-8">
            <Logo />
            <nav className="hidden gap-1 sm:flex">
              {tabs.map((t) => (
                <button
                  type="button"
                  key={t.key}
                  onClick={() => setTab(t.key)}
                  className={`min-h-10 rounded-lg px-3.5 text-sm font-semibold transition ${tab === t.key ? 'bg-slate-900 text-white' : 'text-slate-600 hover:bg-slate-100'}`}
                >
                  {t.label}
                </button>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-2 sm:gap-3">
            <div className="hidden text-right sm:block">
              <p className="max-w-48 truncate text-sm font-medium text-slate-900">{user.email}</p>
            </div>
            <LanguageSelect />
            <Badge tone="indigo">{t(`role.${role}`)}</Badge>
            <Button variant="ghost" onClick={signOut} className="!min-h-10 !px-3">
              {t('nav.signOut')}
            </Button>
          </div>
        </div>
      </header>

      <main className="flex-1 pb-24 sm:pb-8">
        {tab === 'returns' && <MyReturns />}
        {tab === 'queue' && <ReviewQueue />}
        {tab === 'kpis' && (
          <Suspense fallback={<Loading />}>
            <Dashboard />
          </Suspense>
        )}
      </main>

      {tabs.length > 1 && (
        <nav
          className="fixed inset-x-0 bottom-0 z-30 grid border-t border-slate-200 bg-white/95 backdrop-blur safe-bottom sm:hidden"
          style={{ gridTemplateColumns: `repeat(${tabs.length}, 1fr)` }}
        >
          {tabs.map((t) => (
            <button
              type="button"
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`flex min-h-14 flex-col items-center justify-center gap-0.5 text-xs font-semibold ${tab === t.key ? 'text-indigo-600' : 'text-slate-500'}`}
            >
              {t.icon}
              {t.label}
            </button>
          ))}
        </nav>
      )}
    </div>
  )
}
