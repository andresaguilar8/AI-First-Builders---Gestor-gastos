import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { MonthlyExpense } from '../api/expenses'
import { json, mockFetch } from '../test/fetchMock'
import { DueTodayAlert } from './DueTodayAlert'

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date('2026-10-05T15:00:00Z'))
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

const dueToday = (name: string, period = '2026-10'): MonthlyExpense => ({
  expenseId: name.length,
  period,
  kind: 'oneOff',
  name,
  description: null,
  amount: 1000,
  dueDate: '2026-10-05',
  paidOn: null,
  status: 'dueToday',
})

function mockAlert(...expenses: MonthlyExpense[]) {
  return mockFetch(() => json({ date: '2026-10-05', expenses }))
}

describe('DueTodayAlert', () => {
  it('avisa cuando hay un gasto pendiente que vence hoy', async () => {
    // AC-40, AC-41
    const fetchMock = mockAlert(dueToday('Luz'))

    render(<DueTodayAlert refreshKey={0} />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Tenés 1 gasto que vence hoy: Luz.')
    expect(fetchMock).toHaveBeenCalledWith('/api/alerts/due-today', expect.anything())
  })

  it('lista todos los que vencen hoy e indica el mes si no es el actual', async () => {
    mockAlert(dueToday('Tarjeta', '2026-09'), dueToday('Luz'), dueToday('Gas'))

    render(<DueTodayAlert refreshKey={0} />)

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Tenés 3 gastos que vencen hoy: Tarjeta (septiembre de 2026), Luz y Gas.',
    )
  })

  it('no muestra nada si no hay gastos pendientes que venzan hoy', async () => {
    // AC-42: el servidor no incluye los pagados.
    const fetchMock = mockAlert()

    render(<DueTodayAlert refreshKey={0} />)

    await vi.waitFor(() => expect(fetchMock).toHaveBeenCalled())
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('se puede cerrar', async () => {
    mockAlert(dueToday('Luz'))
    const user = userEvent.setup()
    render(<DueTodayAlert refreshKey={0} />)

    await user.click(await screen.findByRole('button', { name: 'Cerrar' }))

    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('vuelve a consultar cuando cambian los gastos', async () => {
    let expenses = [dueToday('Luz')]
    mockFetch(() => json({ date: '2026-10-05', expenses }))
    const { rerender } = render(<DueTodayAlert refreshKey={0} />)
    expect(await screen.findByRole('alert')).toBeInTheDocument()

    expenses = []
    rerender(<DueTodayAlert refreshKey={1} />)

    await vi.waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
  })

  it('no muestra nada si la consulta falla', async () => {
    const fetchMock = mockFetch(() => json({}, 500))

    render(<DueTodayAlert refreshKey={0} />)

    await vi.waitFor(() => expect(fetchMock).toHaveBeenCalled())
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
