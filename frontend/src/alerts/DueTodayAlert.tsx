import { useEffect, useState } from 'react'
import { alertsApi, type DueTodayAlert as Alert } from '../api/alerts'
import { currentPeriod, periodLabel } from '../lib/period'

type Props = {
  /** Cambia cada vez que cambian los gastos, para volver a consultar. */
  refreshKey: number
}

/**
 * Aviso al abrir o recargar la app cuando hay gastos pendientes que vencen hoy
 * (RF-32). Se actualiza cuando cambian los gastos, por ejemplo al pagarlos.
 */
export function DueTodayAlert({ refreshKey }: Props) {
  const [alert, setAlert] = useState<Alert | null>(null)
  const [dismissed, setDismissed] = useState(false)

  useEffect(() => {
    let ignore = false
    alertsApi
      .dueToday()
      .then((result) => !ignore && setAlert(result))
      // Si falla, la app sigue funcionando: solo no se muestra el aviso.
      .catch(() => !ignore && setAlert(null))
    return () => {
      ignore = true
    }
  }, [refreshKey])

  if (dismissed || !alert || alert.expenses.length === 0) {
    return null
  }

  const count = alert.expenses.length
  const thisMonth = currentPeriod()
  const names = alert.expenses.map((expense) =>
    expense.period === thisMonth ? expense.name : `${expense.name} (${periodLabel(expense.period)})`,
  )

  return (
    <div className="due-alert" role="alert">
      <p>
        <strong>{count === 1 ? 'Tenés 1 gasto que vence hoy:' : `Tenés ${count} gastos que vencen hoy:`}</strong>{' '}
        {listInSpanish(names)}.
      </p>
      <button type="button" className="link-button" onClick={() => setDismissed(true)}>
        Cerrar
      </button>
    </div>
  )
}

/** ["Luz", "Gas", "Agua"] → "Luz, Gas y Agua" */
function listInSpanish(items: string[]): string {
  return items.length <= 1 ? items.join('') : `${items.slice(0, -1).join(', ')} y ${items[items.length - 1]}`
}
