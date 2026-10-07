import { useState } from 'react'
import { expensesApi, fieldErrors, type MonthlyExpense } from '../api/expenses'
import { formatAmount } from '../lib/money'
import { formatDate, type Period } from '../lib/period'
import { DueStatusBadge } from './DueStatusBadge'
import { ExpenseForm } from './ExpenseForm'
import { PaymentDateForm } from './PaymentDateForm'

type Props = {
  period: Period
  expense: MonthlyExpense
  /** Algo cambió y hay que volver a pedir la lista (edición, eliminación). */
  onChanged: () => void
  /** El gasto cambió y la API ya devolvió cómo queda (pagos). */
  onUpdated: (expense: MonthlyExpense) => void
}

type Mode = 'view' | 'edit' | 'confirmDelete' | 'payOverdue'

/**
 * Un gasto de la lista. Cualquiera se puede marcar como pagado o desmarcar
 * (RF-27 a RF-31). Los puntuales, además, se pueden editar (RF-09) y eliminar
 * (RF-16); los recurrentes todavía no.
 */
export function ExpenseItem({ period, expense, onChanged, onUpdated }: Props) {
  const [mode, setMode] = useState<Mode>('view')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const editable = expense.kind === 'oneOff'
  const paid = expense.status === 'paid'

  async function run(action: () => Promise<void>, failure: string) {
    setBusy(true)
    setError(null)
    try {
      await action()
    } catch (caught) {
      setError(fieldErrors(caught)?.paidOn ?? failure)
    } finally {
      setBusy(false)
    }
  }

  const pay = (paidOn: string | null) =>
    run(async () => {
      onUpdated(await expensesApi.pay(period, expense.expenseId, paidOn))
      setMode('view')
    }, 'No se pudo marcar como pagado. Probá de nuevo.')

  const unpay = () =>
    run(async () => onUpdated(await expensesApi.unpay(period, expense.expenseId)), 'No se pudo desmarcar el pago. Probá de nuevo.')

  const remove = () =>
    run(async () => {
      await expensesApi.remove(period, expense.expenseId)
      onChanged()
    }, 'No se pudo eliminar el gasto. Probá de nuevo.')

  function startPayment() {
    setError(null)
    // RF-28: si no está vencido, se registra la fecha de hoy sin preguntar.
    if (expense.status === 'overdue') {
      setMode('payOverdue')
    } else {
      void pay(null)
    }
  }

  function changeMode(next: Mode) {
    setError(null)
    setMode(next)
  }

  if (mode === 'edit') {
    return (
      <li className="expense expense--editing">
        <ExpenseForm
          period={period}
          expense={expense}
          onCancel={() => changeMode('view')}
          onSaved={() => {
            changeMode('view')
            onChanged()
          }}
        />
      </li>
    )
  }

  return (
    <li className={`expense expense--${expense.status}`}>
      <div className="expense__main">
        <span className="expense__name">{expense.name}</span>
        <span className={`badge badge--${expense.kind}`}>{expense.kind === 'recurring' ? 'Recurrente' : 'Puntual'}</span>
      </div>
      {expense.description && <p className="expense__description">{expense.description}</p>}
      <div className="expense__meta">
        <span className="expense__amount">{formatAmount(expense.amount)}</span>
        <span className="expense__due">{expense.dueDate ? `Vence ${formatDate(expense.dueDate)}` : 'Sin vencimiento'}</span>
      </div>
      <div className="expense__status">
        <DueStatusBadge expense={expense} />
      </div>

      {mode === 'view' && (
        <div className="expense__actions">
          {paid ? (
            <button type="button" className="link-button" onClick={unpay} disabled={busy} aria-label={`Desmarcar pago de ${expense.name}`}>
              Desmarcar pago
            </button>
          ) : (
            <button type="button" className="small" onClick={startPayment} disabled={busy} aria-label={`Marcar ${expense.name} como pagado`}>
              {busy ? 'Guardando…' : 'Marcar como pagado'}
            </button>
          )}
          {editable && (
            <>
              <button type="button" className="link-button" onClick={() => changeMode('edit')} disabled={busy} aria-label={`Editar ${expense.name}`}>
                Editar
              </button>
              <button
                type="button"
                className="link-button link-button--danger"
                onClick={() => changeMode('confirmDelete')}
                disabled={busy}
                aria-label={`Eliminar ${expense.name}`}
              >
                Eliminar
              </button>
            </>
          )}
        </div>
      )}

      {mode === 'payOverdue' && (
        <PaymentDateForm saving={busy} error={error} onConfirm={pay} onCancel={() => changeMode('view')} />
      )}

      {mode === 'confirmDelete' && (
        <div className="expense__confirm" role="group" aria-label={`Confirmar eliminación de ${expense.name}`}>
          <span>¿Eliminar este gasto?</span>
          <div className="form-actions">
            <button type="button" className="secondary" onClick={() => changeMode('view')} disabled={busy}>
              Cancelar
            </button>
            <button type="button" className="danger" onClick={remove} disabled={busy}>
              {busy ? 'Eliminando…' : 'Sí, eliminar'}
            </button>
          </div>
        </div>
      )}

      {error && mode !== 'payOverdue' && (
        <p role="alert" className="form-error">
          {error}
        </p>
      )}
    </li>
  )
}
