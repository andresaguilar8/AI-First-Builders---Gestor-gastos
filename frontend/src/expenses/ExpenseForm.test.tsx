import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { json, mockFetch } from '../test/fetchMock'
import { ExpenseForm } from './ExpenseForm'

afterEach(() => {
  vi.unstubAllGlobals()
})

function renderForm(period = '2026-10') {
  const onCreated = vi.fn()
  render(<ExpenseForm period={period} onCreated={onCreated} onCancel={vi.fn()} />)
  return { onCreated, user: userEvent.setup() }
}

describe('ExpenseForm', () => {
  it('envía el alta al mes visualizado con el monto convertido', async () => {
    const fetchMock = mockFetch(() => json({}, 201))
    const { onCreated, user } = renderForm('2026-10')

    await user.type(screen.getByLabelText('Nombre'), 'Regalo')
    await user.type(screen.getByLabelText('Monto ($)'), '1.234,56') // AC-06
    await user.click(screen.getByLabelText('Recurrente'))
    fireEvent.change(screen.getByLabelText(/Vencimiento/), { target: { value: '2026-11-05' } }) // AC-07
    await user.type(screen.getByLabelText(/Descripción/), 'Cumpleaños')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(onCreated).toHaveBeenCalled()
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/periods/2026-10/expenses')
    expect(JSON.parse(String(init?.body))).toEqual({
      name: 'Regalo',
      description: 'Cumpleaños',
      amount: 1234.56,
      dueDate: '2026-11-05',
      kind: 'recurring',
    })
  })

  it('manda nulos los campos opcionales vacíos y puntual por defecto', async () => {
    const fetchMock = mockFetch(() => json({}, 201))
    const { user } = renderForm()

    await user.type(screen.getByLabelText('Nombre'), 'Luz')
    await user.type(screen.getByLabelText('Monto ($)'), '15000')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(JSON.parse(String(fetchMock.mock.calls[0][1]?.body))).toEqual({
      name: 'Luz',
      description: null,
      amount: 15000,
      dueDate: null,
      kind: 'oneOff',
    })
  })

  it('no envía un alta sin nombre ni monto', async () => {
    // AC-03
    const fetchMock = mockFetch(() => json({}, 201))
    const { onCreated, user } = renderForm()

    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(screen.getByText('El nombre es obligatorio.')).toBeInTheDocument()
    expect(screen.getByText('El monto es obligatorio.')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
    expect(onCreated).not.toHaveBeenCalled()
  })

  it.each(['0', '-100', '10,999'])('no envía un alta con monto %s', async (amount) => {
    // AC-05
    const fetchMock = mockFetch(() => json({}, 201))
    const { user } = renderForm()

    await user.type(screen.getByLabelText('Nombre'), 'Luz')
    await user.type(screen.getByLabelText('Monto ($)'), amount)
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(screen.getByLabelText('Monto ($)')).toHaveAttribute('aria-invalid', 'true')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('no envía un alta con vencimiento de un mes anterior', async () => {
    // AC-08
    const fetchMock = mockFetch(() => json({}, 201))
    const { user } = renderForm('2026-10')

    await user.type(screen.getByLabelText('Nombre'), 'Tarjeta')
    await user.type(screen.getByLabelText('Monto ($)'), '1000')
    fireEvent.change(screen.getByLabelText(/Vencimiento/), { target: { value: '2026-09-30' } })
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(screen.getByText('El vencimiento no puede ser de un mes anterior al del gasto.')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('muestra junto a cada campo los errores que devuelve la API', async () => {
    mockFetch(() => json({ status: 400, errors: { name: ['El nombre no puede superar los 200 caracteres.'] } }, 400))
    const { onCreated, user } = renderForm()

    await user.type(screen.getByLabelText('Nombre'), 'Luz')
    await user.type(screen.getByLabelText('Monto ($)'), '100')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('El nombre no puede superar los 200 caracteres.')).toBeInTheDocument()
    expect(screen.getByLabelText('Nombre')).toHaveAttribute('aria-invalid', 'true')
    expect(onCreated).not.toHaveBeenCalled()
  })

  it('avisa si no se pudo guardar por un error del servidor', async () => {
    mockFetch(() => json({}, 500))
    const { user } = renderForm()

    await user.type(screen.getByLabelText('Nombre'), 'Luz')
    await user.type(screen.getByLabelText('Monto ($)'), '100')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo guardar el gasto.')
  })
})
