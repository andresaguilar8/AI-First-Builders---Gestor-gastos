import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Category } from '../api/categories'
import { json, mockFetch, requestedUrls } from '../test/fetchMock'
import { CategoriesPage } from './CategoriesPage'

afterEach(() => {
  vi.unstubAllGlobals()
})

const normalize = (name: string) =>
  name
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()

/** Simula la API de categorías, con la regla de nombres repetidos del servidor. */
function mockApi(initial: string[] = []) {
  let nextId = 1
  let categories: Category[] = initial.map((name) => ({ id: nextId++, name }))
  const duplicate = (name: string, id?: number) =>
    categories.some((c) => c.id !== id && normalize(c.name) === normalize(name))
  const nameError = (message: string) => json({ status: 400, errors: { name: [message] } }, 400)

  const fetchMock = mockFetch((url, init) => {
    const id = Number(/\/categories\/(\d+)/.exec(url)?.[1])
    const body = init?.body ? JSON.parse(String(init.body)) : {}
    switch (init?.method ?? 'GET') {
      case 'POST': {
        if (duplicate(body.name)) return nameError('Ya existe una categoría con ese nombre.')
        const created = { id: nextId++, name: body.name }
        categories = [...categories, created]
        return json(created, 201)
      }
      case 'PUT':
        if (duplicate(body.name, id)) return nameError('Ya existe una categoría con ese nombre.')
        categories = categories.map((c) => (c.id === id ? { ...c, name: body.name } : c))
        return json({ id, name: body.name })
      case 'DELETE':
        categories = categories.filter((c) => c.id !== id)
        return new Response(null, { status: 204 })
      default:
        return json(categories)
    }
  })
  return fetchMock
}

const names = () =>
  within(screen.getByRole('list', { name: 'Categorías' }))
    .getAllByRole('listitem')
    .map((item) => item.querySelector('.category__name')?.textContent)

describe('CategoriesPage', () => {
  it('crea una categoría y la muestra en la lista', async () => {
    // AC-25
    mockApi()
    const user = userEvent.setup()
    render(<CategoriesPage />)
    expect(await screen.findByText('Todavía no hay categorías.')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Nueva categoría'), 'Educación')
    await user.click(screen.getByRole('button', { name: 'Agregar' }))

    await waitFor(() => expect(names()).toEqual(['Educación']))
    expect(screen.getByLabelText('Nueva categoría')).toHaveValue('')
  })

  it('muestra el error del servidor si el nombre ya existe', async () => {
    // AC-26
    mockApi(['Educación'])
    const user = userEvent.setup()
    render(<CategoriesPage />)
    await screen.findByText('Educación')

    await user.type(screen.getByLabelText('Nueva categoría'), 'EDUCACION')
    await user.click(screen.getByRole('button', { name: 'Agregar' }))

    expect(await screen.findByText('Ya existe una categoría con ese nombre.')).toBeInTheDocument()
    expect(screen.getByLabelText('Nueva categoría')).toHaveAttribute('aria-invalid', 'true')
    expect(names()).toEqual(['Educación'])
  })

  it('no envía un nombre vacío', async () => {
    const fetchMock = mockApi()
    const user = userEvent.setup()
    render(<CategoriesPage />)
    await screen.findByText('Todavía no hay categorías.')

    await user.click(screen.getByRole('button', { name: 'Agregar' }))

    expect(screen.getByText('El nombre es obligatorio.')).toBeInTheDocument()
    expect(requestedUrls(fetchMock, 'POST')).toEqual([])
  })

  it('renombra una categoría', async () => {
    // AC-28
    mockApi(['Servicios'])
    const user = userEvent.setup()
    render(<CategoriesPage />)

    await user.click(await screen.findByRole('button', { name: 'Renombrar Servicios' }))
    const input = screen.getByLabelText('Nuevo nombre para Servicios')
    expect(input).toHaveValue('Servicios')
    await user.clear(input)
    await user.type(input, 'Servicios del hogar')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    await waitFor(() => expect(names()).toEqual(['Servicios del hogar']))
  })

  it('muestra el error al renombrar con el nombre de otra categoría', async () => {
    // AC-26
    mockApi(['Educación', 'Cursos'])
    const user = userEvent.setup()
    render(<CategoriesPage />)

    await user.click(await screen.findByRole('button', { name: 'Renombrar Cursos' }))
    await user.clear(screen.getByLabelText('Nuevo nombre para Cursos'))
    await user.type(screen.getByLabelText('Nuevo nombre para Cursos'), 'educacion')
    await user.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('Ya existe una categoría con ese nombre.')).toBeInTheDocument()
    expect(screen.getByLabelText('Nuevo nombre para Cursos')).toBeInTheDocument()
  })

  it('elimina una categoría después de confirmar', async () => {
    // AC-29
    const fetchMock = mockApi(['Ocio', 'Varios'])
    const user = userEvent.setup()
    render(<CategoriesPage />)

    await user.click(await screen.findByRole('button', { name: 'Eliminar Ocio' }))
    expect(screen.getByText(/pasarán a "Sin categoría"/)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Sí, eliminar' }))

    await waitFor(() => expect(names()).toEqual(['Varios']))
    expect(requestedUrls(fetchMock, 'DELETE')).toEqual(['/api/categories/1'])
  })

  it('no elimina si se cancela', async () => {
    const fetchMock = mockApi(['Ocio'])
    const user = userEvent.setup()
    render(<CategoriesPage />)

    await user.click(await screen.findByRole('button', { name: 'Eliminar Ocio' }))
    await user.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(names()).toEqual(['Ocio'])
    expect(requestedUrls(fetchMock, 'DELETE')).toEqual([])
  })

  it('permite reintentar si falla la carga', async () => {
    let fail = true
    mockFetch(() => (fail ? json({}, 500) : json([{ id: 1, name: 'Ocio' }])))
    const user = userEvent.setup()
    render(<CategoriesPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudieron cargar las categorías.')
    fail = false
    await user.click(screen.getByRole('button', { name: 'Reintentar' }))

    expect(await screen.findByText('Ocio')).toBeInTheDocument()
  })
})
