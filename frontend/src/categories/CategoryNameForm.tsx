import { useId, useState, type FormEvent } from 'react'
import { fieldErrors } from '../api/client'

type Props = {
  label: string
  submitLabel: string
  initialName?: string
  /** Guarda el nombre. Si falla, el error se muestra junto al campo. */
  onSubmit: (name: string) => Promise<void>
  onCancel?: () => void
}

/** Campo de nombre de categoría, para crear o renombrar (RF-18, RF-20). */
export function CategoryNameForm({ label, submitLabel, initialName = '', onSubmit, onCancel }: Props) {
  const id = useId()
  const [name, setName] = useState(initialName)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (name.trim() === '') {
      setError('El nombre es obligatorio.')
      return
    }

    setSaving(true)
    setError(null)
    try {
      await onSubmit(name.trim())
      setName(initialName)
    } catch (caught) {
      setError(fieldErrors(caught)?.name ?? 'No se pudo guardar la categoría. Probá de nuevo.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="category-form" onSubmit={handleSubmit} noValidate>
      <div className="field">
        <label htmlFor={`${id}-name`}>{label}</label>
        <div className="category-form__row">
          <input
            id={`${id}-name`}
            value={name}
            maxLength={100}
            onChange={(event) => setName(event.target.value)}
            aria-invalid={Boolean(error)}
            aria-describedby={error ? `${id}-error` : undefined}
          />
          {onCancel && (
            <button type="button" className="secondary" onClick={onCancel} disabled={saving}>
              Cancelar
            </button>
          )}
          <button type="submit" disabled={saving}>
            {saving ? 'Guardando…' : submitLabel}
          </button>
        </div>
        {error && (
          <span id={`${id}-error`} className="field__error">
            {error}
          </span>
        )}
      </div>
    </form>
  )
}
