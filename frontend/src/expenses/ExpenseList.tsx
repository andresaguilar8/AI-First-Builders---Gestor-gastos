import type { MonthlyExpense } from '../api/expenses'
import type { Period } from '../lib/period'
import { ExpenseItem } from './ExpenseItem'

type Props = {
  period: Period
  expenses: MonthlyExpense[]
  onChanged: () => void
}

export function ExpenseList({ period, expenses, onChanged }: Props) {
  if (expenses.length === 0) {
    return <p className="empty">No hay gastos en este mes.</p>
  }

  return (
    <ul className="expense-list" aria-label="Gastos del mes">
      {expenses.map((expense) => (
        <ExpenseItem key={expense.expenseId} period={period} expense={expense} onChanged={onChanged} />
      ))}
    </ul>
  )
}
