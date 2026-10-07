import { render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { MonthlySummary } from '../api/summary'
import { json, mockFetch } from '../test/fetchMock'
import { MonthSummary } from './MonthSummary'

afterEach(() => {
  vi.unstubAllGlobals()
})

// Intl separa el símbolo con un espacio no separable, y según el entorno escribe
// "67%" o "67 %": se normalizan ambos.
const normalize = (value: string | null | undefined) => value?.replace(/\s/g, ' ').replace(/(\d) %/g, '$1%')
const text = (element: HTMLElement) => normalize(element.textContent)

const september: MonthlySummary = {
  period: '2026-09',
  total: 450000,
  pending: 250000,
  byCategory: [
    { category: { id: 1, name: 'Vivienda' }, total: 300000, count: 2 },
    { category: { id: 2, name: 'Servicios' }, total: 100000, count: 1 },
    { category: null, total: 50000, count: 1 },
  ],
}

function rows() {
  return within(screen.getByRole('list')).getAllByRole('listitem')
}

describe('MonthSummary', () => {
  it('muestra el total del mes y el pendiente de pago', async () => {
    // AC-34
    mockFetch(() => json({ ...september, total: 400000, pending: 100000 }))

    render(<MonthSummary period="2026-09" refreshKey="a" />)

    const total = await screen.findByText('Total del mes')
    expect(text(total.nextElementSibling as HTMLElement)).toBe('$ 400.000,00')
    expect(text(screen.getByText('Pendiente de pago').nextElementSibling as HTMLElement)).toBe('$ 100.000,00')
  })

  it('muestra el total de cada categoría, incluido "Sin categoría"', async () => {
    // AC-32, AC-33
    const fetchMock = mockFetch(() => json(september))

    render(<MonthSummary period="2026-09" refreshKey="a" />)

    await screen.findByText('Vivienda')
    expect(fetchMock).toHaveBeenCalledWith('/api/periods/2026-09/summary', expect.anything())
    expect(rows().map((row) => text(row.querySelector('.category-total__row') as HTMLElement))).toEqual([
      'Vivienda$ 300.000,0067%',
      'Servicios$ 100.000,0022%',
      'Sin categoría$ 50.000,0011%',
    ])
  })

  it('dibuja cada barra en proporción a la categoría más grande', async () => {
    mockFetch(() => json(september))

    render(<MonthSummary period="2026-09" refreshKey="a" />)

    await screen.findByText('Vivienda')
    const widths = rows().map((row) => (row.querySelector('.category-total__bar') as HTMLElement).style.width)
    expect(widths.map((w) => Math.round(parseFloat(w)))).toEqual([100, 33, 17])
  })

  it('da el detalle de cada categoría al pasar el mouse', async () => {
    mockFetch(() => json(september))

    render(<MonthSummary period="2026-09" refreshKey="a" />)

    await screen.findByText('Vivienda')
    expect(normalize(rows()[0].getAttribute('title'))).toBe('Vivienda: $ 300.000,00, 67% del mes, 2 gastos')
  })

  it('en un mes sin gastos muestra los totales en cero y sin categorías', async () => {
    mockFetch(() => json({ period: '2026-10', total: 0, pending: 0, byCategory: [] }))

    render(<MonthSummary period="2026-10" refreshKey="a" />)

    expect(text((await screen.findByText('Total del mes')).nextElementSibling as HTMLElement)).toBe('$ 0,00')
    expect(screen.queryByText('Por categoría')).not.toBeInTheDocument()
  })

  it('vuelve a consultar cuando cambia la clave y conserva el resumen mientras tanto', async () => {
    let pending = 250000
    const fetchMock = mockFetch(() => json({ ...september, pending }))
    const { rerender } = render(<MonthSummary period="2026-09" refreshKey="a" />)
    await screen.findByText('Vivienda')

    pending = 0
    rerender(<MonthSummary period="2026-09" refreshKey="b" />)

    expect(screen.getByText('Vivienda')).toBeInTheDocument()
    await vi.waitFor(() =>
      expect(text(screen.getByText('Pendiente de pago').nextElementSibling as HTMLElement)).toBe('$ 0,00'),
    )
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('avisa si no se pudo cargar', async () => {
    mockFetch(() => json({}, 500))

    render(<MonthSummary period="2026-09" refreshKey="a" />)

    expect(await screen.findByText('No se pudo cargar el resumen.')).toBeInTheDocument()
  })
})
