import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, ApiError } from './client'

function mockFetch(status: number, body?: unknown) {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(body === undefined ? null : JSON.stringify(body), { status }),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('api client', () => {
  it('prefija /api y devuelve el JSON de la respuesta', async () => {
    const fetchMock = mockFetch(200, { status: 'ok' })

    const result = await api.get<{ status: string }>('/health')

    expect(result).toEqual({ status: 'ok' })
    expect(fetchMock).toHaveBeenCalledWith('/api/health', expect.objectContaining({ method: 'GET' }))
  })

  it('envía el cuerpo como JSON', async () => {
    const fetchMock = mockFetch(201, { id: 1 })

    await api.post('/expenses', { name: 'Alquiler' })

    const [, init] = fetchMock.mock.calls[0]
    expect(init.body).toBe(JSON.stringify({ name: 'Alquiler' }))
    expect(init.headers).toEqual({ 'Content-Type': 'application/json' })
  })

  it('acepta respuestas sin cuerpo', async () => {
    mockFetch(204)

    await expect(api.delete('/expenses/1')).resolves.toBeUndefined()
  })

  it('lanza ApiError con el status y el cuerpo cuando la respuesta no es exitosa', async () => {
    mockFetch(400, { errors: { amount: ['inválido'] } })

    const error = await api.post('/expenses', {}).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 400, body: { errors: { amount: ['inválido'] } } })
  })
})
