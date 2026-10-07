import type { Period } from '../lib/period'
import { api, ApiError } from './client'

export type ExpenseKind = 'oneOff' | 'recurring'

/** Un gasto tal como se ve en un mes (proyectado o con valores propios). */
export type MonthlyExpense = {
  expenseId: number
  period: Period
  kind: ExpenseKind
  name: string
  description: string | null
  amount: number
  dueDate: string | null
  paidOn: string | null
}

export type NewExpense = {
  name: string
  description: string | null
  amount: number
  dueDate: string | null
  kind: ExpenseKind
}

export const expensesApi = {
  list: (period: Period) => api.get<MonthlyExpense[]>(`/periods/${period}/expenses`),
  create: (period: Period, expense: NewExpense) =>
    api.post<MonthlyExpense>(`/periods/${period}/expenses`, expense),
}

/**
 * Los errores por campo de una respuesta 400 de validación, con el primer
 * mensaje de cada campo. Null si el error es de otro tipo.
 */
export function fieldErrors(error: unknown): Record<string, string> | null {
  if (!(error instanceof ApiError) || error.status !== 400) {
    return null
  }

  const errors = (error.body as { errors?: Record<string, string[]> } | undefined)?.errors
  if (!errors) {
    return null
  }

  return Object.fromEntries(Object.entries(errors).map(([field, messages]) => [field, messages[0]]))
}
