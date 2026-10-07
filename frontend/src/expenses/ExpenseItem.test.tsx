import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { DueStatus, MonthlyExpense } from '../api/expenses'
import { json, mockFetch } from '../test/fetchMock'
import { ExpenseItem } from './ExpenseItem'

beforeEach(() => {
  // Hoy: 05/10/2026 en Buenos Aires.
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date('2026-10-05T15:00:00Z'))
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

const expense = (overrides: Partial<MonthlyExpense> = {}): MonthlyExpense => ({
  expenseId: 4,
  period: '2026-10',
  kind: 'recurring',
  name: 'Luz',
  description: null,
  amount: 20000,
  dueDate: '2026-10-10',
  paidOn: null,
  status: 'upToDate',
  ...overrides,
})

const overdue = expense({ dueDate: '2026-10-01', status: 'overdue' })

function renderItem(item: MonthlyExpense) {
  const onUpdated = vi.fn()
  render(
    <ul>
      <ExpenseItem period="2026-10" expense={item} onChanged={vi.fn()} onUpdated={onUpdated} />
    </ul>,
  )
  return { onUpdated, user: userEvent.setup() }
}

/** La API devuelve el gasto pagado con la fecha pedida, o la de hoy. */
function mockPayment() {
  return mockFetch((_, init) => {
    const paidOn = JSON.parse(String(init?.body)).paidOn ?? '2026-10-05'
    return json({ ...overdue, paidOn, status: 'paid' })
  })
}

function sentPaidOn(fetchMock: ReturnType<typeof mockFetch>) {
  const [url, init] = fetchMock.mock.calls[0]
  expect(url).toBe('/api/periods/2026-10/expenses/4/payment')
  expect(init?.method).toBe('PUT')
  return JSON.parse(String(init?.body)).paidOn
}

describe('ExpenseItem', () => {
  it.each<[DueStatus, string]>([
    ['upToDate', 'Al día'], // AC-43, AC-44
    ['dueSoon', 'Próximo a vencer'], // AC-45
    ['dueToday', 'Vence hoy'], // AC-46
    ['overdue', 'Vencido'], // AC-47
  ])('muestra el estado %s como "%s"', (status, label) => {
    renderItem(expense({ status }))

    expect(screen.getByText(label)).toHaveClass(`status--${status}`)
  })

  it('muestra la fecha de pago de un gasto pagado', () => {
    renderItem(expense({ paidOn: '2026-10-03', status: 'paid' }))

    expect(screen.getByText('Pagado el 03/10/2026')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Desmarcar pago de Luz' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Marcar Luz como pagado' })).not.toBeInTheDocument()
  })

  it('paga un gasto no vencido sin preguntar la fecha', async () => {
    // AC-35
    const fetchMock = mockPayment()
    const { onUpdated, user } = renderItem(expense())

    await user.click(screen.getByRole('button', { name: 'Marcar Luz como pagado' }))

    expect(sentPaidOn(fetchMock)).toBeNull()
    expect(screen.queryByText(/¿Lo pagaste hoy\?/)).not.toBeInTheDocument()
    expect(onUpdated).toHaveBeenCalledWith(expect.objectContaining({ paidOn: '2026-10-05', status: 'paid' }))
  })

  it('pregunta si un gasto vencido se pagó hoy y lo paga con la fecha actual', async () => {
    // AC-37
    const fetchMock = mockPayment()
    const { onUpdated, user } = renderItem(overdue)

    await user.click(screen.getByRole('button', { name: 'Marcar Luz como pagado' }))
    expect(fetchMock).not.toHaveBeenCalled()
    await user.click(screen.getByRole('button', { name: 'Sí, hoy' }))

    expect(sentPaidOn(fetchMock)).toBeNull()
    expect(onUpdated).toHaveBeenCalledWith(expect.objectContaining({ paidOn: '2026-10-05' }))
  })

  it('permite indicar la fecha real de pago de un gasto vencido', async () => {
    // AC-36
    const fetchMock = mockPayment()
    const { onUpdated, user } = renderItem(overdue)

    await user.click(screen.getByRole('button', { name: 'Marcar Luz como pagado' }))
    await user.click(screen.getByRole('button', { name: 'No, otro día' }))
    expect(screen.getByLabelText('¿Qué día lo pagaste?')).toHaveAttribute('max', '2026-10-05')
    fireEvent.change(screen.getByLabelText('¿Qué día lo pagaste?'), { target: { value: '2026-10-03' } })
    await user.click(screen.getByRole('button', { name: 'Confirmar pago' }))

    expect(sentPaidOn(fetchMock)).toBe('2026-10-03')
    expect(onUpdated).toHaveBeenCalledWith(expect.objectContaining({ paidOn: '2026-10-03' }))
  })

  it('rechaza una fecha de pago futura y el gasto sigue pendiente', async () => {
    // AC-38
    const fetchMock = mockPayment()
    const { onUpdated, user } = renderItem(overdue)

    await user.click(screen.getByRole('button', { name: 'Marcar Luz como pagado' }))
    await user.click(screen.getByRole('button', { name: 'No, otro día' }))
    fireEvent.change(screen.getByLabelText('¿Qué día lo pagaste?'), { target: { value: '2026-10-06' } })
    await user.click(screen.getByRole('button', { name: 'Confirmar pago' }))

    expect(screen.getByRole('alert')).toHaveTextContent('La fecha de pago no puede ser posterior a hoy.')
    expect(fetchMock).not.toHaveBeenCalled()
    expect(onUpdated).not.toHaveBeenCalled()
  })

  it('cancela el pago de un gasto vencido', async () => {
    const fetchMock = mockPayment()
    const { user } = renderItem(overdue)

    await user.click(screen.getByRole('button', { name: 'Marcar Luz como pagado' }))
    await user.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(screen.getByRole('button', { name: 'Marcar Luz como pagado' })).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('muestra el error de la API sobre la fecha de pago', async () => {
    mockFetch(() => json({ status: 400, errors: { paidOn: ['La fecha de pago no puede ser posterior a hoy.'] } }, 400))
    const { onUpdated, user } = renderItem(overdue)

    await user.click(screen.getByRole('button', { name: 'Marcar Luz como pagado' }))
    await user.click(screen.getByRole('button', { name: 'Sí, hoy' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('La fecha de pago no puede ser posterior a hoy.')
    expect(onUpdated).not.toHaveBeenCalled()
  })

  it('avisa si no se pudo marcar como pagado', async () => {
    mockFetch(() => json({}, 500))
    const { user } = renderItem(expense())

    await user.click(screen.getByRole('button', { name: 'Marcar Luz como pagado' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo marcar como pagado.')
  })

  it('desmarca un pago', async () => {
    // AC-39
    const fetchMock = mockFetch(() => json(expense()))
    const { onUpdated, user } = renderItem(expense({ paidOn: '2026-10-05', status: 'paid' }))

    await user.click(screen.getByRole('button', { name: 'Desmarcar pago de Luz' }))

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/periods/2026-10/expenses/4/payment')
    expect(init?.method).toBe('DELETE')
    expect(onUpdated).toHaveBeenCalledWith(expect.objectContaining({ paidOn: null, status: 'upToDate' }))
  })

  it('ofrece pagar los recurrentes pero no editarlos ni eliminarlos', () => {
    renderItem(expense({ kind: 'recurring' }))

    expect(screen.getByRole('button', { name: 'Marcar Luz como pagado' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Editar Luz' })).not.toBeInTheDocument()
  })
})
