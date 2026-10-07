import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { MonthlyExpense } from '../api/expenses'
import { ExpenseList } from './ExpenseList'

const expense = (overrides: Partial<MonthlyExpense>): MonthlyExpense => ({
  expenseId: 1,
  period: '2026-10',
  kind: 'oneOff',
  name: 'Regalo',
  description: null,
  amount: 15000,
  dueDate: null,
  paidOn: null,
  ...overrides,
})

describe('ExpenseList', () => {
  it('muestra nombre, tipo, monto, descripción y vencimiento de cada gasto', () => {
    render(
      <ExpenseList
        expenses={[
          expense({ expenseId: 1, name: 'Alquiler', kind: 'recurring', amount: 300000, dueDate: '2026-11-05' }),
          expense({ expenseId: 2, name: 'Regalo', description: 'Cumpleaños', amount: 1234.56 }),
        ]}
      />,
    )

    const [rent, gift] = screen.getAllByRole('listitem')
    expect(within(rent).getByText('Alquiler')).toBeInTheDocument()
    expect(within(rent).getByText('Recurrente')).toBeInTheDocument()
    expect(within(rent).getByText(/300\.000,00/)).toBeInTheDocument()
    expect(within(rent).getByText('Vence 05/11/2026')).toBeInTheDocument()

    expect(within(gift).getByText('Puntual')).toBeInTheDocument()
    expect(within(gift).getByText('Cumpleaños')).toBeInTheDocument()
    expect(within(gift).getByText(/1\.234,56/)).toBeInTheDocument()
    expect(within(gift).getByText('Sin vencimiento')).toBeInTheDocument()
  })

  it('avisa cuando el mes no tiene gastos', () => {
    render(<ExpenseList expenses={[]} />)

    expect(screen.getByText('No hay gastos en este mes.')).toBeInTheDocument()
  })
})
