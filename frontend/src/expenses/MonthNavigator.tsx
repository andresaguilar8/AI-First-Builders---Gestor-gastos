import { useState } from 'react'
import { addMonths, currentPeriod, makePeriod, monthName, parsePeriod, periodLabel, type Period } from '../lib/period'

type Props = {
  period: Period
  onChange: (period: Period) => void
}

const MONTHS = Array.from({ length: 12 }, (_, index) => index + 1)

/** Navegación entre meses y años (RF-35). */
export function MonthNavigator({ period, onChange }: Props) {
  const { year, month } = parsePeriod(period)
  const [yearText, setYearText] = useState(String(year))
  const [shownYear, setShownYear] = useState(year)
  const today = currentPeriod()

  // Si el año cambia desde afuera (flechas, "Ir al mes actual"), se refleja en el campo.
  if (shownYear !== year) {
    setShownYear(year)
    setYearText(String(year))
  }

  function changeYear(text: string) {
    setYearText(text)
    if (/^\d{4}$/.test(text) && Number(text) >= 1) {
      onChange(makePeriod(Number(text), month))
    }
  }

  return (
    <nav className="month-nav" aria-label="Navegación de meses">
      <button type="button" className="icon-button" onClick={() => onChange(addMonths(period, -1))} aria-label="Mes anterior">
        ‹
      </button>

      <h2 className="month-nav__title">{periodLabel(period)}</h2>

      <button type="button" className="icon-button" onClick={() => onChange(addMonths(period, 1))} aria-label="Mes siguiente">
        ›
      </button>

      <div className="month-nav__jump">
        <select aria-label="Mes" value={month} onChange={(event) => onChange(makePeriod(year, Number(event.target.value)))}>
          {MONTHS.map((value) => (
            <option key={value} value={value}>
              {monthName(value)}
            </option>
          ))}
        </select>
        <input
          aria-label="Año"
          inputMode="numeric"
          maxLength={4}
          value={yearText}
          onChange={(event) => changeYear(event.target.value)}
          onBlur={() => setYearText(String(year))}
        />
        {period !== today && (
          <button type="button" className="link-button" onClick={() => onChange(today)}>
            Ir al mes actual
          </button>
        )}
      </div>
    </nav>
  )
}
