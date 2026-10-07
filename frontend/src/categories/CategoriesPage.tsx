import { useEffect, useState } from 'react'
import { categoriesApi, type Category } from '../api/categories'
import { CategoryItem } from './CategoryItem'
import { CategoryNameForm } from './CategoryNameForm'

type Result = { status: 'error' } | { status: 'ready'; categories: Category[] }

/** Alta, renombrado y eliminación de categorías (RF-18 a RF-21). */
type Props = {
  /** Se llama cuando se crea, renombra o elimina una categoría. */
  onChanged?: () => void
}

export function CategoriesPage({ onChanged }: Props = {}) {
  const [reloadKey, setReloadKey] = useState(0)
  const [result, setResult] = useState<(Result & { key: number }) | null>(null)
  const state = result?.key === reloadKey ? result : { status: 'loading' as const }
  const reload = () => {
    onChanged?.()
    setReloadKey((key) => key + 1)
  }

  useEffect(() => {
    let ignore = false
    categoriesApi
      .list()
      .then((categories) => !ignore && setResult({ key: reloadKey, status: 'ready', categories }))
      .catch(() => !ignore && setResult({ key: reloadKey, status: 'error' }))
    return () => {
      ignore = true
    }
  }, [reloadKey])

  return (
    <section className="categories-page" aria-labelledby="categories-title">
      <h2 id="categories-title">Categorías</h2>

      <CategoryNameForm
        label="Nueva categoría"
        submitLabel="Agregar"
        onSubmit={async (name) => {
          await categoriesApi.create(name)
          reload()
        }}
      />

      {state.status === 'loading' && <p className="empty">Cargando categorías…</p>}
      {state.status === 'error' && (
        <div role="alert" className="form-error">
          No se pudieron cargar las categorías.{' '}
          <button type="button" className="link-button" onClick={reload}>
            Reintentar
          </button>
        </div>
      )}
      {state.status === 'ready' &&
        (state.categories.length === 0 ? (
          <p className="empty">Todavía no hay categorías.</p>
        ) : (
          <ul className="category-list" aria-label="Categorías">
            {state.categories.map((category) => (
              <CategoryItem key={category.id} category={category} onChanged={reload} />
            ))}
          </ul>
        ))}
    </section>
  )
}
