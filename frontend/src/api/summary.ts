import type { Period } from '../lib/period'
import { api } from './client'

export type CategoryTotal = {
  /** Null es el grupo "Sin categoría" (RF-24). */
  category: { id: number; name: string } | null
  total: number
  count: number
}

/** Resumen del mes: total, pendiente y total por categoría (RF-23 a RF-26). */
export type MonthlySummary = {
  period: Period
  total: number
  pending: number
  byCategory: CategoryTotal[]
}

export const summaryApi = {
  get: (period: Period) => api.get<MonthlySummary>(`/periods/${period}/summary`),
}
