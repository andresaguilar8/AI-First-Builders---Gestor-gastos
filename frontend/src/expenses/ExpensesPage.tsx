import { useEffect, useState } from 'react'
import { expensesApi, type MonthlyExpense } from '../api/expenses'
import { currentPeriod, type Period } from '../lib/period'
import { ExpenseForm } from './ExpenseForm'
import { ExpenseList } from './ExpenseList'
import { MonthNavigator } from './MonthNavigator'

type Result = { status: 'error' } | { status: 'ready'; expenses: MonthlyExpense[] }

export function ExpensesPage() {
  // RF-34: se arranca en el mes actual.
  const [period, setPeriod] = useState<Period>(() => currentPeriod())
  const [reloadKey, setReloadKey] = useState(0)
  const [adding, setAdding] = useState(false)

  // Cada resultado queda asociado a la carga que lo pidió. Mientras no llegue
  // el de la carga actual, la lista está cargando.
  const loadKey = `${period}#${reloadKey}`
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

  function changePeriod(next: Period) {
    setPeriod(next)
    setAdding(false)
  }

  return (
    <section className="expenses-page">
      <MonthNavigator period={period} onChange={changePeriod} />

      {adding ? (
        <ExpenseForm
          key={period}
          period={period}
          onCancel={() => setAdding(false)}
          onCreated={() => {
            setAdding(false)
            setReloadKey((key) => key + 1)
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
      {state.status === 'ready' && <ExpenseList expenses={state.expenses} />}
    </section>
  )
}
