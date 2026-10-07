import { useId, useState, type FormEvent } from 'react'
import { todayInBuenosAires } from '../lib/period'

type Props = {
  saving: boolean
  error: string | null
  onConfirm: (paidOn: string | null) => void
  onCancel: () => void
}

/**
 * Para un gasto vencido: pregunta si se pagó hoy y, si no, pide la fecha real
 * de pago, que no puede ser futura (RF-29, RF-30).
 */
export function PaymentDateForm({ saving, error, onConfirm, onCancel }: Props) {
  const id = useId()
  const today = todayInBuenosAires()
  const [pickingDate, setPickingDate] = useState(false)
  const [paidOn, setPaidOn] = useState('')
  const [dateError, setDateError] = useState<string | null>(null)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (paidOn === '') {
      setDateError('Indicá la fecha de pago.')
    } else if (paidOn > today) {
      setDateError('La fecha de pago no puede ser posterior a hoy.')
    } else {
      setDateError(null)
      onConfirm(paidOn === today ? null : paidOn)
    }
  }

  const shownError = dateError ?? error

  if (!pickingDate) {
    return (
      <div className="expense__confirm" role="group" aria-label="Fecha de pago">
        <span>Este gasto está vencido. ¿Lo pagaste hoy?</span>
        <div className="form-actions">
          <button type="button" className="secondary" onClick={onCancel} disabled={saving}>
            Cancelar
          </button>
          <button type="button" className="secondary" onClick={() => setPickingDate(true)} disabled={saving}>
            No, otro día
          </button>
          <button type="button" onClick={() => onConfirm(null)} disabled={saving}>
            Sí, hoy
          </button>
        </div>
        {error && (
          <p role="alert" className="form-error">
            {error}
          </p>
        )}
      </div>
    )
  }

  return (
    <form className="expense__confirm" onSubmit={handleSubmit} noValidate aria-label="Fecha de pago">
      <div className="field">
        <label htmlFor={`${id}-paidOn`}>¿Qué día lo pagaste?</label>
        <input
          id={`${id}-paidOn`}
          type="date"
          max={today}
          value={paidOn}
          onChange={(event) => setPaidOn(event.target.value)}
          aria-invalid={Boolean(shownError)}
          aria-describedby={shownError ? `${id}-paidOn-error` : undefined}
        />
        {shownError && (
          <span id={`${id}-paidOn-error`} role="alert" className="field__error">
            {shownError}
          </span>
        )}
      </div>
      <div className="form-actions">
        <button type="button" className="secondary" onClick={onCancel} disabled={saving}>
          Cancelar
        </button>
        <button type="submit" disabled={saving}>
          {saving ? 'Guardando…' : 'Confirmar pago'}
        </button>
      </div>
    </form>
  )
}
