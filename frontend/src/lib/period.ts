/** Un mes calendario en formato "aaaa-mm", igual que en la API. */
export type Period = string

export const TIME_ZONE = 'America/Argentina/Buenos_Aires'

const MONTH_NAMES = [
  'enero',
  'febrero',
  'marzo',
  'abril',
  'mayo',
  'junio',
  'julio',
  'agosto',
  'septiembre',
  'octubre',
  'noviembre',
  'diciembre',
] as const

export function makePeriod(year: number, month: number): Period {
  return `${String(year).padStart(4, '0')}-${String(month).padStart(2, '0')}`
}

export function parsePeriod(period: Period): { year: number; month: number } {
  const [year, month] = period.split('-').map(Number)
  return { year, month }
}

/** Fecha de hoy ("aaaa-mm-dd") en Buenos Aires, sin importar la zona del navegador (RNF-07). */
export function todayInBuenosAires(now: Date = new Date()): string {
  // en-CA formatea como aaaa-mm-dd.
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: TIME_ZONE,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(now)
}

/** Mes actual en Buenos Aires (RF-34, RNF-07). */
export function currentPeriod(now: Date = new Date()): Period {
  return todayInBuenosAires(now).slice(0, 7)
}

export function addMonths(period: Period, months: number): Period {
  const { year, month } = parsePeriod(period)
  const index = year * 12 + (month - 1) + months
  return makePeriod(Math.floor(index / 12), (index % 12) + 1)
}

export function monthName(month: number): string {
  return MONTH_NAMES[month - 1]
}

/** "octubre de 2026" */
export function periodLabel(period: Period): string {
  const { year, month } = parsePeriod(period)
  return `${monthName(month)} de ${year}`
}

/** Primer día del mes ("aaaa-mm-01"), el vencimiento más temprano permitido (RF-05). */
export function firstDay(period: Period): string {
  return `${period}-01`
}

/** "2026-10-05" → "05/10/2026" */
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-')
  return `${day}/${month}/${year}`
}
