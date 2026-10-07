import { describe, expect, it } from 'vitest'
import { addMonths, currentPeriod, formatDate, periodLabel, todayInBuenosAires } from './period'

describe('period', () => {
  it('usa la fecha de Buenos Aires aunque en UTC ya sea el día siguiente', () => {
    // AC-48: 22:00 del 05/10/2026 en Buenos Aires = 01:00 del 06/10/2026 UTC.
    expect(todayInBuenosAires(new Date('2026-10-06T01:00:00Z'))).toBe('2026-10-05')
  })

  it('el mes actual es el de Buenos Aires en el cambio de mes', () => {
    // 31/10/2026 23:30 en Buenos Aires = 01/11/2026 02:30 UTC.
    expect(currentPeriod(new Date('2026-11-01T02:30:00Z'))).toBe('2026-10')
  })

  it('navega entre meses y años', () => {
    expect(addMonths('2026-09', -1)).toBe('2026-08') // AC-50
    expect(addMonths('2027-01', -1)).toBe('2026-12') // AC-51
    expect(addMonths('2026-12', 1)).toBe('2027-01')
    expect(addMonths('2026-10', -24)).toBe('2024-10')
  })

  it('muestra el mes en castellano', () => {
    expect(periodLabel('2026-10')).toBe('octubre de 2026')
  })

  it('formatea fechas como dd/mm/aaaa', () => {
    expect(formatDate('2026-11-05')).toBe('05/11/2026')
  })
})
