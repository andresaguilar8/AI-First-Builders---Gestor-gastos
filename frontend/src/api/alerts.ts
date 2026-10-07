import type { MonthlyExpense } from './expenses'
import { api } from './client'

/** Los gastos pendientes que vencen hoy (RF-32). Vacía si no hay ninguno. */
export type DueTodayAlert = {
  date: string
  expenses: MonthlyExpense[]
}

export const alertsApi = {
  dueToday: () => api.get<DueTodayAlert>('/alerts/due-today'),
}
