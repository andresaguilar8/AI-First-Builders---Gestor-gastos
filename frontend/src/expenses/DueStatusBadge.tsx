import type { MonthlyExpense } from '../api/expenses'
import { formatDate } from '../lib/period'

const LABELS = {
  upToDate: 'Al día',
  dueSoon: 'Próximo a vencer',
  dueToday: 'Vence hoy',
  overdue: 'Vencido',
} as const

/** El estado del gasto respecto de su vencimiento, o la fecha de pago si está pagado (RF-33). */
export function DueStatusBadge({ expense }: { expense: MonthlyExpense }) {
  const label =
    expense.status === 'paid'
      ? expense.paidOn
        ? `Pagado el ${formatDate(expense.paidOn)}`
        : 'Pagado'
      : LABELS[expense.status]

  return <span className={`status status--${expense.status}`}>{label}</span>
}
