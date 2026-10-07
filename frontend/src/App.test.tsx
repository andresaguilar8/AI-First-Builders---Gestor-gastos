import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { MonthlyExpense } from './api/expenses'
import App from './App'
import { json, mockFetch } from './test/fetchMock'

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date('2026-10-05T15:00:00Z'))
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

describe('App', () => {
  it('al abrir avisa del gasto que vence hoy y deja de avisar al pagarlo', async () => {
    // AC-40 / AC-41 al abrir, AC-42 una vez pagado.
    let luz: MonthlyExpense = {
      expenseId: 1,
      period: '2026-10',
      kind: 'oneOff',
      name: 'Luz',
      description: null,
      amount: 1000,
      dueDate: '2026-10-05',
      paidOn: null,
      status: 'dueToday',
    }
    mockFetch((url, init) => {
      if (url.endsWith('/payment') && init?.method === 'PUT') {
        luz = { ...luz, paidOn: '2026-10-05', status: 'paid' }
        return json(luz)
      }
      if (url === '/api/alerts/due-today') {
        return json({ date: '2026-10-05', expenses: luz.paidOn ? [] : [luz] })
      }
      return json([luz])
    })
    const user = userEvent.setup()

    render(<App />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Tenés 1 gasto que vence hoy: Luz.')

    await user.click(await screen.findByRole('button', { name: 'Marcar Luz como pagado' }))

    expect(await screen.findByText('Pagado el 05/10/2026')).toBeInTheDocument()
    await vi.waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
  })

  it('cambia entre gastos y categorías sin perder el mes elegido', async () => {
    mockFetch((url) => {
      if (url === '/api/alerts/due-today') return json({ date: '2026-10-05', expenses: [] })
      if (url === '/api/categories') return json([{ id: 1, name: 'Vivienda' }])
      return json([])
    })
    const user = userEvent.setup()
    render(<App />)
    await user.click(screen.getByRole('button', { name: 'Mes siguiente' }))

    await user.click(screen.getByRole('button', { name: 'Categorías' }))

    expect(await screen.findByText('Vivienda')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Categorías' })).toHaveAttribute('aria-current', 'page')
    expect(screen.queryByRole('heading', { name: 'noviembre de 2026' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Gastos' }))

    expect(screen.getByRole('heading', { name: 'noviembre de 2026' })).toBeInTheDocument()
  })
})
