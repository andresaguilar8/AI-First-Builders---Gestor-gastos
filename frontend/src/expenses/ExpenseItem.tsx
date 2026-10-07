import { useState } from 'react'
import { expensesApi, type MonthlyExpense } from '../api/expenses'
import { formatAmount } from '../lib/money'
import { formatDate, type Period } from '../lib/period'
import { ExpenseForm } from './ExpenseForm'

type Props = {
  period: Period
  expense: MonthlyExpense
  onChanged: () => void
}

type Mode = 'view' | 'edit' | 'confirmDelete'

/**
 * Un gasto de la lista. Los puntuales se pueden editar (RF-09) y eliminar
 * (RF-16); los recurrentes, por ahora, solo se muestran.
 */
export function ExpenseItem({ period, expense, onChanged }: Props) {
  const [mode, setMode] = useState<Mode>('view')
  const [deleting, setDeleting] = useState(false)
  const [deleteFailed, setDeleteFailed] = useState(false)
  const editable = expense.kind === 'oneOff'

  async function handleDelete() {
    setDeleting(true)
    setDeleteFailed(false)
    try {
      await expensesApi.remove(period, expense.expenseId)
      onChanged()
    } catch {
      setDeleteFailed(true)
      setDeleting(false)
    }
  }

  if (mode === 'edit') {
    return (
      <li className="expense expense--editing">
        <ExpenseForm
          period={period}
          expense={expense}
          onCancel={() => setMode('view')}
          onSaved={() => {
            setMode('view')
            onChanged()
          }}
        />
      </li>
    )
  }

  return (
    <li className="expense">
      <div className="expense__main">
        <span className="expense__name">{expense.name}</span>
        <span className={`badge badge--${expense.kind}`}>{expense.kind === 'recurring' ? 'Recurrente' : 'Puntual'}</span>
      </div>
      {expense.description && <p className="expense__description">{expense.description}</p>}
      <div className="expense__meta">
        <span className="expense__amount">{formatAmount(expense.amount)}</span>
        <span className="expense__due">{expense.dueDate ? `Vence ${formatDate(expense.dueDate)}` : 'Sin vencimiento'}</span>
      </div>

      {editable && mode === 'view' && (
        <div className="expense__actions">
          <button type="button" className="link-button" onClick={() => setMode('edit')} aria-label={`Editar ${expense.name}`}>
            Editar
          </button>
          <button
            type="button"
            className="link-button link-button--danger"
            onClick={() => setMode('confirmDelete')}
            aria-label={`Eliminar ${expense.name}`}
          >
            Eliminar
          </button>
        </div>
      )}

      {mode === 'confirmDelete' && (
        <div className="expense__confirm" role="group" aria-label={`Confirmar eliminación de ${expense.name}`}>
          <span>¿Eliminar este gasto?</span>
          <div className="form-actions">
            <button type="button" className="secondary" onClick={() => setMode('view')} disabled={deleting}>
              Cancelar
            </button>
            <button type="button" className="danger" onClick={handleDelete} disabled={deleting}>
              {deleting ? 'Eliminando…' : 'Sí, eliminar'}
            </button>
          </div>
          {deleteFailed && (
            <p role="alert" className="form-error">
              No se pudo eliminar el gasto. Probá de nuevo.
            </p>
          )}
        </div>
      )}
    </li>
  )
}
