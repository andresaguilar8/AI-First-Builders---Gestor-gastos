import { useState } from 'react'
import { categoriesApi, type Category } from '../api/categories'
import { CategoryNameForm } from './CategoryNameForm'

type Props = {
  category: Category
  onChanged: () => void
}

type Mode = 'view' | 'rename' | 'confirmDelete'

export function CategoryItem({ category, onChanged }: Props) {
  const [mode, setMode] = useState<Mode>('view')
  const [deleting, setDeleting] = useState(false)
  const [deleteFailed, setDeleteFailed] = useState(false)

  async function handleDelete() {
    setDeleting(true)
    setDeleteFailed(false)
    try {
      await categoriesApi.remove(category.id)
      onChanged()
    } catch {
      setDeleteFailed(true)
      setDeleting(false)
    }
  }

  if (mode === 'rename') {
    return (
      <li className="category">
        <CategoryNameForm
          label={`Nuevo nombre para ${category.name}`}
          submitLabel="Guardar"
          initialName={category.name}
          onCancel={() => setMode('view')}
          onSubmit={async (name) => {
            await categoriesApi.rename(category.id, name)
            setMode('view')
            onChanged()
          }}
        />
      </li>
    )
  }

  return (
    <li className="category">
      <div className="category__main">
        <span className="category__name">{category.name}</span>
        {mode === 'view' && (
          <div className="category__actions">
            <button type="button" className="link-button" onClick={() => setMode('rename')} aria-label={`Renombrar ${category.name}`}>
              Renombrar
            </button>
            <button
              type="button"
              className="link-button link-button--danger"
              onClick={() => setMode('confirmDelete')}
              aria-label={`Eliminar ${category.name}`}
            >
              Eliminar
            </button>
          </div>
        )}
      </div>

      {mode === 'confirmDelete' && (
        <div className="expense__confirm" role="group" aria-label={`Confirmar eliminación de ${category.name}`}>
          <span>¿Eliminar esta categoría? Sus gastos, de todos los meses, pasarán a "Sin categoría".</span>
          <div className="form-actions">
            <button type="button" className="secondary" onClick={() => setMode('view')} disabled={deleting}>
              Cancelar
            </button>
            <button type="button" className="danger" onClick={handleDelete} disabled={deleting}>
              {deleting ? 'Eliminando…' : 'Sí, eliminar'}
            </button>
          </div>
          {deleteFailed && (
            <p role="alert" className="form-error">
              No se pudo eliminar la categoría. Probá de nuevo.
            </p>
          )}
        </div>
      )}
    </li>
  )
}
