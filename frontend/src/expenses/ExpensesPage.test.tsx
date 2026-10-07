import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { MonthlyExpense } from '../api/expenses'
import { json, mockFetch, requestedUrls } from '../test/fetchMock'
import { ExpensesPage } from './ExpensesPage'

function setNow(isoInstant: string) {
  vi.setSystemTime(new Date(isoInstant))
}

beforeEach(() => {
  vi.useFakeTimers({ toFake: ['Date'] })
  setNow('2026-10-05T15:00:00Z')
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

const expense = (name: string, period: string): MonthlyExpense => ({
  expenseId: name.length,
  period,
  kind: 'oneOff',
  name,
  description: null,
  amount: 1000,
  dueDate: null,
  paidOn: null,
  status: 'upToDate',
})

/** Simula la API con los gastos de cada mes: alta, edición y eliminación. */
function mockApi(byPeriod: Record<string, MonthlyExpense[]> = {}) {
  return mockFetch((url, init) => {
    const [, period = '', id, payment] = /\/periods\/(\d{4}-\d{2})\/expenses(?:\/(\d+))?(\/payment)?/.exec(url) ?? []
    const expenses = byPeriod[period] ?? []
    if (payment) {
      const paidOn = init?.method === 'PUT' ? (JSON.parse(String(init.body)).paidOn ?? '2026-10-05') : null
      byPeriod[period] = expenses.map((e) =>
        e.expenseId === Number(id) ? { ...e, paidOn, status: paidOn ? 'paid' : 'upToDate' } : e,
      )
      return json(byPeriod[period].find((e) => e.expenseId === Number(id)))
    }
    switch (init?.method ?? 'GET') {
      case 'POST': {
        const created = expense(JSON.parse(String(init?.body)).name, period)
        byPeriod[period] = [...expenses, created]
        return json(created, 201)
      }
      case 'PUT': {
        const changes = JSON.parse(String(init?.body))
        byPeriod[period] = expenses.map((e) => (e.expenseId === Number(id) ? { ...e, ...changes } : e))
        return json(byPeriod[period].find((e) => e.expenseId === Number(id)))
      }
      case 'DELETE':
        byPeriod[period] = expenses.filter((e) => e.expenseId !== Number(id))
        return new Response(null, { status: 204 })
      default:
        return json(expenses)
    }
  })
}

describe('ExpensesPage', () => {
  it('arranca mostrando los gastos del mes actual', async () => {
    // AC-49
    const fetchMock = mockApi({ '2026-10': [expense('Alquiler', '2026-10')] })

    render(<ExpensesPage />)

    expect(screen.getByRole('heading', { name: 'octubre de 2026' })).toBeInTheDocument()
    expect(await screen.findByText('Alquiler')).toBeInTheDocument()
    expect(requestedUrls(fetchMock)).toEqual(['/api/periods/2026-10/expenses'])
  })

  it('usa el mes de Buenos Aires aunque en UTC ya sea el mes siguiente', async () => {
    setNow('2026-11-01T02:30:00Z') // 31/10 23:30 en Buenos Aires
    mockApi()

    render(<ExpensesPage />)

    expect(screen.getByRole('heading', { name: 'octubre de 2026' })).toBeInTheDocument()
    expect(await screen.findByText('No hay gastos en este mes.')).toBeInTheDocument()
  })

  it('navega al mes anterior', async () => {
    // AC-50
    setNow('2026-09-15T15:00:00Z')
    const fetchMock = mockApi({ '2026-08': [expense('Gimnasio', '2026-08')] })
    const user = userEvent.setup()
    render(<ExpensesPage />)

    await user.click(screen.getByRole('button', { name: 'Mes anterior' }))

    expect(screen.getByRole('heading', { name: 'agosto de 2026' })).toBeInTheDocument()
    expect(await screen.findByText('Gimnasio')).toBeInTheDocument()
    expect(requestedUrls(fetchMock)).toContain('/api/periods/2026-08/expenses')
  })

  it('pasa de enero a diciembre del año anterior', async () => {
    // AC-51
    setNow('2027-01-10T15:00:00Z')
    mockApi()
    const user = userEvent.setup()
    render(<ExpensesPage />)

    await user.click(screen.getByRole('button', { name: 'Mes anterior' }))

    expect(screen.getByRole('heading', { name: 'diciembre de 2026' })).toBeInTheDocument()
  })

  it('salta a otro mes y año y vuelve al mes actual', async () => {
    const fetchMock = mockApi()
    const user = userEvent.setup()
    render(<ExpensesPage />)

    await user.selectOptions(screen.getByLabelText('Mes'), 'marzo')
    await user.clear(screen.getByLabelText('Año'))
    await user.type(screen.getByLabelText('Año'), '2020')

    expect(screen.getByRole('heading', { name: 'marzo de 2020' })).toBeInTheDocument()
    expect(requestedUrls(fetchMock)).toContain('/api/periods/2020-03/expenses')

    await user.click(screen.getByRole('button', { name: 'Ir al mes actual' }))

    expect(screen.getByRole('heading', { name: 'octubre de 2026' })).toBeInTheDocument()
    expect(screen.getByLabelText('Año')).toHaveValue('2026')
    expect(screen.queryByRole('button', { name: 'Ir al mes actual' })).not.toBeInTheDocument()
  })

  it('agrega un gasto al mes visualizado y lo muestra en la lista', async () => {
    // AC-04: hoy es octubre, se visualiza diciembre.
    const fetchMock = mockApi()
    const user = userEvent.setup()
    render(<ExpensesPage />)
    await user.click(screen.getByRole('button', { name: 'Mes siguiente' }))
    await user.click(screen.getByRole('button', { name: 'Mes siguiente' }))

    await user.click(screen.getByRole('button', { name: '+ Agregar gasto' }))
    const form = screen.getByRole('form', { name: 'Nuevo gasto' })
    expect(within(form).getByText('Nuevo gasto en diciembre de 2026')).toBeInTheDocument()
    await user.type(within(form).getByLabelText('Nombre'), 'Seguro')
    await user.type(within(form).getByLabelText('Monto ($)'), '5.000')
    await user.click(within(form).getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('Seguro')).toBeInTheDocument()
    expect(screen.queryByRole('form', { name: 'Nuevo gasto' })).not.toBeInTheDocument()
    expect(requestedUrls(fetchMock, 'POST')).toEqual(['/api/periods/2026-12/expenses'])
  })

  it('permite reintentar si falla la carga', async () => {
    let fail = true
    mockFetch(() => (fail ? json({}, 500) : json([expense('Luz', '2026-10')])))
    const user = userEvent.setup()
    render(<ExpensesPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudieron cargar los gastos.')

    fail = false
    await user.click(screen.getByRole('button', { name: 'Reintentar' }))

    expect(await screen.findByText('Luz')).toBeInTheDocument()
  })

  it('edita un gasto puntual y muestra los nuevos valores', async () => {
    // AC-14
    mockApi({ '2026-10': [expense('Regalo', '2026-10')] })
    const user = userEvent.setup()
    render(<ExpensesPage />)

    await user.click(await screen.findByRole('button', { name: 'Editar Regalo' }))
    const form = screen.getByRole('form', { name: 'Editar gasto' })
    await user.clear(within(form).getByLabelText('Nombre'))
    await user.type(within(form).getByLabelText('Nombre'), 'Regalo de cumpleaños')
    await user.click(within(form).getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('Regalo de cumpleaños')).toBeInTheDocument()
    expect(screen.queryByRole('form', { name: 'Editar gasto' })).not.toBeInTheDocument()
  })

  it('elimina un gasto puntual después de confirmar', async () => {
    // AC-23
    const fetchMock = mockApi({ '2026-10': [expense('Regalo', '2026-10'), expense('Luz', '2026-10')] })
    const user = userEvent.setup()
    render(<ExpensesPage />)

    await user.click(await screen.findByRole('button', { name: 'Eliminar Regalo' }))
    expect(requestedUrls(fetchMock, 'DELETE')).toEqual([])
    await user.click(screen.getByRole('button', { name: 'Sí, eliminar' }))

    await waitFor(() => expect(screen.queryByText('Regalo')).not.toBeInTheDocument())
    expect(screen.getByText('Luz')).toBeInTheDocument()
    expect(requestedUrls(fetchMock, 'DELETE')).toEqual(['/api/periods/2026-10/expenses/6'])
  })

  it('no elimina si se cancela la confirmación', async () => {
    const fetchMock = mockApi({ '2026-10': [expense('Regalo', '2026-10')] })
    const user = userEvent.setup()
    render(<ExpensesPage />)

    await user.click(await screen.findByRole('button', { name: 'Eliminar Regalo' }))
    await user.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(screen.getByText('Regalo')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Eliminar Regalo' })).toBeInTheDocument()
    expect(requestedUrls(fetchMock, 'DELETE')).toEqual([])
  })

  it('avisa si no se pudo eliminar', async () => {
    mockFetch((_, init) => (init?.method === 'DELETE' ? json({}, 500) : json([expense('Regalo', '2026-10')])))
    const user = userEvent.setup()
    render(<ExpensesPage />)

    await user.click(await screen.findByRole('button', { name: 'Eliminar Regalo' }))
    await user.click(screen.getByRole('button', { name: 'Sí, eliminar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo eliminar el gasto.')
    expect(screen.getByText('Regalo')).toBeInTheDocument()
  })

  it('marca un gasto como pagado sin volver a pedir la lista', async () => {
    // AC-35
    const fetchMock = mockApi({ '2026-10': [expense('Luz', '2026-10')] })
    const user = userEvent.setup()
    render(<ExpensesPage />)

    await user.click(await screen.findByRole('button', { name: 'Marcar Luz como pagado' }))

    expect(await screen.findByText('Pagado el 05/10/2026')).toBeInTheDocument()
    expect(requestedUrls(fetchMock)).toEqual(['/api/periods/2026-10/expenses'])
    expect(requestedUrls(fetchMock, 'PUT')).toEqual(['/api/periods/2026-10/expenses/3/payment'])
  })

  it('desmarca un pago sin volver a pedir la lista', async () => {
    // AC-39
    const fetchMock = mockApi({ '2026-10': [{ ...expense('Luz', '2026-10'), paidOn: '2026-10-05', status: 'paid' }] })
    const user = userEvent.setup()
    render(<ExpensesPage />)

    await user.click(await screen.findByRole('button', { name: 'Desmarcar pago de Luz' }))

    expect(await screen.findByRole('button', { name: 'Marcar Luz como pagado' })).toBeInTheDocument()
    expect(screen.queryByText(/Pagado el/)).not.toBeInTheDocument()
    expect(requestedUrls(fetchMock)).toEqual(['/api/periods/2026-10/expenses'])
  })
})
