import { useEffect, useState } from 'react'
import { categoriesApi, type Category } from '../api/categories'
import { expensesApi, type MonthlyExpense } from '../api/expenses'
import { currentPeriod, type Period } from '../lib/period'
import { MonthSummary } from '../summary/MonthSummary'
import { ExpenseForm } from './ExpenseForm'
import { ExpenseList } from './ExpenseList'
import { MonthNavigator } from './MonthNavigator'

type Result = { status: 'error' } | { status: 'ready'; expenses: MonthlyExpense[] }

type Props = {
  /** Se llama cuando se crea, edita, elimina o paga un gasto. */
  onExpensesChanged?: () => void
  /** Cambia cuando cambian las categorías: hay que volver a cargarlas, y los gastos con sus nombres. */
  categoriesKey?: number
}

export function ExpensesPage({ onExpensesChanged, categoriesKey = 0 }: Props = {}) {
  // RF-34: se arranca en el mes actual.
  const [period, setPeriod] = useState<Period>(() => currentPeriod())
  const [reloadKey, setReloadKey] = useState(0)
  const [adding, setAdding] = useState(false)
  // Pagar o desmarcar no recarga la lista, pero sí cambia el pendiente del resumen.
  const [paymentsVersion, setPaymentsVersion] = useState(0)

  // Cada resultado queda asociado a la carga que lo pidió. Mientras no llegue
  // el de la carga actual, la lista está cargando.
  const loadKey = `${period}#${reloadKey}#${categoriesKey}`
  const [result, setResult] = useState<(Result & { key: string }) | null>(null)
  const state = result?.key === loadKey ? result : { status: 'loading' as const }

  useEffect(() => {
    let ignore = false
    expensesApi
      .list(period)
      .then((expenses) => !ignore && setResult({ key: loadKey, status: 'ready', expenses }))
      .catch(() => !ignore && setResult({ key: loadKey, status: 'error' }))
    return () => {
      ignore = true
    }
  }, [period, loadKey])

  // Si no se pueden cargar las categorías, el formulario solo ofrece "Sin categoría".
  const [categories, setCategories] = useState<Category[]>([])
  useEffect(() => {
    let ignore = false
    categoriesApi
      .list()
      .then((list) => !ignore && setCategories(list))
      .catch(() => !ignore && setCategories([]))
    return () => {
      ignore = true
    }
  }, [categoriesKey])

  // Pagar o desmarcar no vuelve a pedir la lista: se reemplaza el gasto con
  // lo que devolvió la API (AC-35: sin recargar la página).
  function replaceExpense(updated: MonthlyExpense) {
    onExpensesChanged?.()
    setPaymentsVersion((version) => version + 1)
    setResult((current) =>
      current?.status === 'ready'
        ? { ...current, expenses: current.expenses.map((e) => (e.expenseId === updated.expenseId ? updated : e)) }
        : current,
    )
  }

  function reload() {
    onExpensesChanged?.()
    setReloadKey((key) => key + 1)
  }

  function changePeriod(next: Period) {
    setPeriod(next)
    setAdding(false)
  }

  return (
    <section className="expenses-page">
      <MonthNavigator period={period} onChange={changePeriod} />

      <MonthSummary period={period} refreshKey={`${loadKey}#${paymentsVersion}`} />

      {adding ? (
        <ExpenseForm
          key={period}
          period={period}
          categories={categories}
          onCancel={() => setAdding(false)}
          onSaved={() => {
            setAdding(false)
            reload()
          }}
        />
      ) : (
        <button type="button" className="add-button" onClick={() => setAdding(true)}>
          + Agregar gasto
        </button>
      )}

      {state.status === 'loading' && <p className="empty">Cargando gastos…</p>}
      {state.status === 'error' && (
        <div role="alert" className="form-error">
          No se pudieron cargar los gastos.{' '}
          <button type="button" className="link-button" onClick={() => setReloadKey((key) => key + 1)}>
            Reintentar
          </button>
        </div>
      )}
      {state.status === 'ready' && (
        <ExpenseList
          period={period}
          expenses={state.expenses}
          categories={categories}
          onChanged={reload}
          onUpdated={replaceExpense}
        />
      )}
    </section>
  )
}
