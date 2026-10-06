import { render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('App', () => {
  it('muestra el estado del servidor', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(JSON.stringify({ status: 'ok', database: 'ok', today: '2026-10-06' })),
      ),
    )

    render(<App />)

    expect(await screen.findByText(/Base de datos: ok/)).toBeInTheDocument()
  })

  it('avisa cuando no puede conectar con el servidor', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    render(<App />)

    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo conectar con el servidor.')
  })
})
