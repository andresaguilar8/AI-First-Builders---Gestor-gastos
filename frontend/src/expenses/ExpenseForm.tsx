import { useId, useState, type FormEvent } from 'react'
import { fieldErrors } from '../api/client'
import { expensesApi, type ExpenseKind, type MonthlyExpense } from '../api/expenses'
import { formatAmountInput, parseAmount } from '../lib/money'
import { firstDay, periodLabel, type Period } from '../lib/period'

type Props = {
  period: Period
  /** El gasto a editar. Sin él, el formulario da de alta uno nuevo. */
  expense?: MonthlyExpense
  onSaved: () => void
  onCancel: () => void
}

type Errors = Partial<Record<'name' | 'amount' | 'description' | 'dueDate' | 'kind' | 'form', string>>

/** Alta (RF-01 a RF-05) o edición (RF-09) de un gasto del mes visualizado. */
export function ExpenseForm({ period, expense, onSaved, onCancel }: Props) {
  const id = useId()
  const editing = expense !== undefined
  const [name, setName] = useState(expense?.name ?? '')
  const [amount, setAmount] = useState(expense ? formatAmountInput(expense.amount) : '')
  const [description, setDescription] = useState(expense?.description ?? '')
  const [dueDate, setDueDate] = useState(expense?.dueDate ?? '')
  const [kind, setKind] = useState<ExpenseKind>('oneOff')
  const [errors, setErrors] = useState<Errors>({})
  const [saving, setSaving] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const nextErrors: Errors = {}
    if (name.trim() === '') {
      nextErrors.name = 'El nombre es obligatorio.'
    }
    const parsedAmount = parseAmount(amount)
    if (!parsedAmount.ok) {
      nextErrors.amount = parsedAmount.error
    }
    if (dueDate !== '' && dueDate < firstDay(period)) {
      nextErrors.dueDate = 'El vencimiento no puede ser de un mes anterior al del gasto.'
    }

    setErrors(nextErrors)
    if (Object.keys(nextErrors).length > 0 || !parsedAmount.ok) {
      return
    }

    const values = {
      name: name.trim(),
      description: description.trim() || null,
      amount: parsedAmount.value,
      dueDate: dueDate || null,
    }

    setSaving(true)
    try {
      if (editing) {
        await expensesApi.update(period, expense.expenseId, values)
      } else {
        await expensesApi.create(period, { ...values, kind })
      }
      onSaved()
    } catch (error) {
      setErrors(fieldErrors(error) ?? { form: 'No se pudo guardar el gasto. Probá de nuevo.' })
    } finally {
      setSaving(false)
    }
  }

  const fieldError = (field: keyof Errors) =>
    errors[field] && (
      <span id={`${id}-${field}-error`} className="field__error">
        {errors[field]}
      </span>
    )

  const describedBy = (field: keyof Errors) => (errors[field] ? `${id}-${field}-error` : undefined)

  return (
    <form className="expense-form" onSubmit={handleSubmit} noValidate aria-label={editing ? 'Editar gasto' : 'Nuevo gasto'}>
      <h3>{editing ? `Editar ${expense.name}` : `Nuevo gasto en ${periodLabel(period)}`}</h3>

      <div className="field">
        <label htmlFor={`${id}-name`}>Nombre</label>
        <input
          id={`${id}-name`}
          value={name}
          maxLength={200}
          onChange={(event) => setName(event.target.value)}
          aria-invalid={Boolean(errors.name)}
          aria-describedby={describedBy('name')}
        />
        {fieldError('name')}
      </div>

      <div className="field">
        <label htmlFor={`${id}-amount`}>Monto ($)</label>
        <input
          id={`${id}-amount`}
          value={amount}
          inputMode="decimal"
          placeholder="1.234,56"
          onChange={(event) => setAmount(event.target.value)}
          aria-invalid={Boolean(errors.amount)}
          aria-describedby={describedBy('amount')}
        />
        {fieldError('amount')}
      </div>

      {!editing && (
        <fieldset className="field">
          <legend>Tipo</legend>
          <div className="radio-group">
            <label>
              <input type="radio" name={`${id}-kind`} checked={kind === 'oneOff'} onChange={() => setKind('oneOff')} />
              Puntual
            </label>
            <label>
              <input type="radio" name={`${id}-kind`} checked={kind === 'recurring'} onChange={() => setKind('recurring')} />
              Recurrente
            </label>
          </div>
          {fieldError('kind')}
        </fieldset>
      )}

      <div className="field">
        <label htmlFor={`${id}-dueDate`}>
          Vencimiento <span className="field__optional">(opcional)</span>
        </label>
        <input
          id={`${id}-dueDate`}
          type="date"
          min={firstDay(period)}
          value={dueDate}
          onChange={(event) => setDueDate(event.target.value)}
          aria-invalid={Boolean(errors.dueDate)}
          aria-describedby={describedBy('dueDate')}
        />
        {fieldError('dueDate')}
      </div>

      <div className="field">
        <label htmlFor={`${id}-description`}>
          Descripción <span className="field__optional">(opcional)</span>
        </label>
        <textarea
          id={`${id}-description`}
          rows={2}
          maxLength={1000}
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          aria-invalid={Boolean(errors.description)}
          aria-describedby={describedBy('description')}
        />
        {fieldError('description')}
      </div>

      {errors.form && (
        <p role="alert" className="form-error">
          {errors.form}
        </p>
      )}

      <div className="form-actions">
        <button type="button" className="secondary" onClick={onCancel} disabled={saving}>
          Cancelar
        </button>
        <button type="submit" disabled={saving}>
          {saving ? 'Guardando…' : 'Guardar'}
        </button>
      </div>
    </form>
  )
}
