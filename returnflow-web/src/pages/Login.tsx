import { useQuery } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Button, ErrorNote, Field, inputClass, Logo } from '../components/ui'
import { api } from '../lib/api'
import { useAuth } from '../lib/auth'
import { LanguageSelect, useI18n } from '../lib/i18n'

export default function Login() {
  const { signIn, signUp } = useAuth()
  const { t } = useI18n()
  const config = useQuery({ queryKey: ['config'], queryFn: api.config, retry: false })
  const [mode, setMode] = useState<'in' | 'up'>('in')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function run(action: () => Promise<void>) {
    setBusy(true)
    setError('')
    try {
      await action()
    } catch (e) {
      setError(e instanceof Error ? e.message : t('error.generic'))
    } finally {
      setBusy(false)
    }
  }

  const submit = (e: FormEvent) => {
    e.preventDefault()
    void run(() => (mode === 'in' ? signIn(email, password) : signUp(email, password)))
  }

  const demo = config.data?.demoMode ? config.data : null

  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <section className="relative hidden overflow-hidden bg-slate-900 p-12 text-white lg:flex lg:flex-col lg:justify-between">
        <div className="absolute -right-24 -top-24 size-96 rounded-full bg-indigo-600/30 blur-3xl" />
        <div className="absolute -bottom-32 -left-16 size-96 rounded-full bg-sky-500/20 blur-3xl" />
        <Logo light />
        <div className="relative max-w-md">
          <h1 className="text-4xl font-bold leading-tight tracking-tight">{t('login.headline')}</h1>
          <p className="mt-4 text-lg text-slate-300">{t('login.intro')}</p>
          <ul className="mt-8 space-y-3 text-slate-200">
            {['login.benefit1', 'login.benefit2', 'login.benefit3'].map((key) => (
              <li key={key} className="flex items-center gap-3">
                <span className="grid size-6 place-items-center rounded-full bg-sky-400/20 text-sky-300">&#10003;</span>
                {t(key)}
              </li>
            ))}
          </ul>
        </div>
        <p className="relative text-sm text-slate-400">{t('login.demoNotice')}</p>
      </section>

      <section className="flex items-center justify-center px-5 py-10 safe-top">
        <div className="rf-fade-up w-full max-w-sm">
          <div className="mb-8 flex items-center justify-between gap-3">
            <div className="lg:hidden">
              <Logo />
            </div>
            <div className="ml-auto">
              <LanguageSelect />
            </div>
          </div>
          <h2 className="text-2xl font-bold tracking-tight text-slate-900">
            {mode === 'in' ? t('login.welcome') : t('login.createTitle')}
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            {mode === 'in' ? t('login.signInDescription') : t('login.signUpDescription')}
          </p>

          <form onSubmit={submit} className="mt-6 space-y-4">
            <Field label={t('login.email')}>
              <input
                className={inputClass}
                type="email"
                autoComplete="email"
                inputMode="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
              />
            </Field>
            <Field label={t('login.password')} hint={mode === 'up' ? t('login.passwordHint') : undefined}>
              <input
                className={inputClass}
                type="password"
                autoComplete={mode === 'in' ? 'current-password' : 'new-password'}
                required
                minLength={mode === 'up' ? 8 : undefined}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </Field>
            {error && <ErrorNote message={error} />}
            <Button type="submit" busy={busy} className="w-full">
              {mode === 'in' ? t('login.signIn') : t('login.createAccount')}
            </Button>
          </form>

          <p className="mt-5 text-center text-sm text-slate-500">
            {mode === 'in' ? t('login.newHere') : t('login.alreadyAccount')}{' '}
            <button
              className="font-semibold text-indigo-600 hover:underline"
              onClick={() => {
                setMode(mode === 'in' ? 'up' : 'in')
                setError('')
              }}
            >
              {mode === 'in' ? t('login.createAccount') : t('login.signIn')}
            </button>
          </p>

          {demo && (
            <div className="mt-8 rounded-2xl border border-indigo-100 bg-indigo-50/60 p-4">
              <p className="text-sm font-semibold text-indigo-900">{t('login.tryDemo')}</p>
              <p className="mt-0.5 text-xs text-indigo-700/80">{t('login.demoDescription')}</p>
              <div className="mt-3 grid grid-cols-3 gap-2">
                {demo.demoAccounts.map((a) => (
                  <Button
                    key={a.email}
                    variant="secondary"
                    disabled={busy}
                    className="!min-h-10 !px-2 text-xs"
                    onClick={() => void run(() => signIn(a.email, demo.demoPassword ?? ''))}
                  >
                    {t(`role.${a.role}`)}
                  </Button>
                ))}
              </div>
            </div>
          )}
        </div>
      </section>
    </div>
  )
}
