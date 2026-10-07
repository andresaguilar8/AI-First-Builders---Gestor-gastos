import type { MonthlyExpense } from '../api/expenses'
import { formatAmount } from '../lib/money'
import { formatDate } from '../lib/period'

type Props = {
  expenses: MonthlyExpense[]
}

export function ExpenseList({ expenses }: Props) {
  if (expenses.length === 0) {
    return <p className="empty">No hay gastos en este mes.</p>
  }

  return (
    <ul className="expense-list" aria-label="Gastos del mes">
      {expenses.map((expense) => (
        <li key={expense.expenseId} className="expense">
          <div className="expense__main">
            <span className="expense__name">{expense.name}</span>
            <span className={`badge badge--${expense.kind}`}>
              {expense.kind === 'recurring' ? 'Recurrente' : 'Puntual'}
            </span>
          </div>
          {expense.description && <p className="expense__description">{expense.description}</p>}
          <div className="expense__meta">
            <span className="expense__amount">{formatAmount(expense.amount)}</span>
            <span className="expense__due">
              {expense.dueDate ? `Vence ${formatDate(expense.dueDate)}` : 'Sin vencimiento'}
            </span>
          </div>
        </li>
      ))}
    </ul>
  )
}
