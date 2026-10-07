import { useEffect, useState } from 'react'
import { summaryApi, type MonthlySummary } from '../api/summary'
import { formatAmount } from '../lib/money'
import type { Period } from '../lib/period'

type Props = {
  period: Period
  /** Cambia cada vez que cambian los gastos o las categorías, para volver a consultar. */
  refreshKey: string
}

const percent = new Intl.NumberFormat('es-AR', { style: 'percent', maximumFractionDigits: 0 })

/** Total del mes, pendiente de pago y total por categoría (RF-23 a RF-26). */
export function MonthSummary({ period, refreshKey }: Props) {
  const [result, setResult] = useState<{ key: string; summary: MonthlySummary | null } | null>(null)

  useEffect(() => {
    let ignore = false
    summaryApi
      .get(period)
      .then((summary) => !ignore && setResult({ key: refreshKey, summary }))
      .catch(() => !ignore && setResult({ key: refreshKey, summary: null }))
    return () => {
      ignore = true
    }
  }, [period, refreshKey])

  // Mientras se actualiza el mismo mes se sigue mostrando el resumen anterior,
  // así los totales no parpadean al pagar un gasto.
  const summary = result?.summary?.period === period ? result.summary : null

  if (!summary) {
    return (
      <section className="summary" aria-label="Resumen del mes">
        <p className="empty">{result && result.key === refreshKey ? 'No se pudo cargar el resumen.' : 'Cargando resumen…'}</p>
      </section>
    )
  }

  const largest = Math.max(0, ...summary.byCategory.map((group) => group.total))

  return (
    <section className="summary" aria-label="Resumen del mes">
      <dl className="summary__tiles">
        <div className="summary__tile">
          <dt>Total del mes</dt>
          <dd>{formatAmount(summary.total)}</dd>
        </div>
        <div className="summary__tile">
          <dt>Pendiente de pago</dt>
          <dd>{formatAmount(summary.pending)}</dd>
        </div>
      </dl>

      {summary.byCategory.length > 0 && (
        <>
          <h3 className="summary__title">Por categoría</h3>
          <ul className="category-totals">
            {summary.byCategory.map((group) => {
              const name = group.category?.name ?? 'Sin categoría'
              const share = summary.total > 0 ? group.total / summary.total : 0
              const detail = `${name}: ${formatAmount(group.total)}, ${percent.format(share)} del mes, ${group.count} ${
                group.count === 1 ? 'gasto' : 'gastos'
              }`
              return (
                <li key={group.category?.id ?? 'none'} className="category-total" title={detail}>
                  <div className="category-total__row">
                    <span className={`category-total__name${group.category ? '' : ' category-total__name--none'}`}>{name}</span>
                    <span className="category-total__amount">{formatAmount(group.total)}</span>
                    <span className="category-total__share">{percent.format(share)}</span>
                  </div>
                  <div className="category-total__track" aria-hidden="true">
                    <div className="category-total__bar" style={{ width: `${largest > 0 ? (group.total / largest) * 100 : 0}%` }} />
                  </div>
                </li>
              )
            })}
          </ul>
        </>
      )}
    </section>
  )
}
