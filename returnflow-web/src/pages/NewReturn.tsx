import { useQueryClient } from '@tanstack/react-query'
import { type FormEvent, useState } from 'react'
import { Button, ErrorNote, Field, inputClass, Sheet } from '../components/ui'
import { api } from '../lib/api'
import { todayIso } from '../lib/format'
import { useI18n } from '../lib/i18n'

export default function NewReturn({ onClose, onDone }: { onClose: () => void; onDone: () => void }) {
  const { t } = useI18n()
  const qc = useQueryClient()
  const [orderReference, setOrderReference] = useState('')
  const [purchaseDate, setPurchaseDate] = useState(todayIso())
  const [reason, setReason] = useState('')
  const [description, setDescription] = useState('')
  const [amount, setAmount] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError('')
    try {
      const created = await api.createReturn({ orderReference, purchaseDate, reason })
      await api.addClaim(created.id, { description, amountRequested: Number(amount) })
      await qc.invalidateQueries({ queryKey: ['returns'] })
      onDone()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('returnForm.submitError'))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Sheet title={t('returnForm.title')} onClose={onClose}>
      <form onSubmit={submit} className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label={t('returnForm.order')}>
            <input
              className={inputClass}
              required
              maxLength={50}
              value={orderReference}
              onChange={(e) => setOrderReference(e.target.value)}
              placeholder="ORD-1234"
            />
          </Field>
          <Field label={t('returnForm.purchaseDate')} hint={t('returnForm.windowHint')}>
            <input
              className={inputClass}
              type="date"
              required
              max={todayIso()}
              value={purchaseDate}
              onChange={(e) => setPurchaseDate(e.target.value)}
            />
          </Field>
        </div>
        <Field label={t('returnForm.item')}>
          <input
            className={inputClass}
            required
            maxLength={500}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Wireless headphones"
          />
        </Field>
        <Field label={t('returnForm.description')} hint={t('returnForm.descriptionHint')}>
          <textarea
            className={`${inputClass} min-h-28`}
            required
            maxLength={1000}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="The left earcup arrived cracked."
          />
        </Field>
        <Field label={t('returnForm.amount')}>
          <input
            className={inputClass}
            type="number"
            inputMode="decimal"
            required
            min="0.01"
            max="100000"
            step="0.01"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="49.90"
          />
        </Field>
        {error && <ErrorNote message={error} />}
        <div className="flex gap-3 pt-1">
          <Button type="button" variant="secondary" onClick={onClose} className="flex-1">
            {t('common.cancel')}
          </Button>
          <Button type="submit" busy={busy} className="flex-1">
            {t('common.submitClaim')}
          </Button>
        </div>
      </form>
    </Sheet>
  )
}
