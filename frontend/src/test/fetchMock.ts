import { vi } from 'vitest'

type Handler = (url: string, init: RequestInit | undefined) => Response | Promise<Response>

/** Reemplaza fetch por un handler que recibe la URL y las opciones del pedido. */
export function mockFetch(handler: Handler) {
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => Promise.resolve(handler(String(input), init)))
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status })
}

/** Las URLs pedidas con un método dado, en orden. */
export function requestedUrls(fetchMock: ReturnType<typeof mockFetch>, method = 'GET') {
  return fetchMock.mock.calls.filter(([, init]) => (init?.method ?? 'GET') === method).map(([url]) => String(url))
}
