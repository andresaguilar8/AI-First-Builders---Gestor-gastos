/**
 * Montos en pesos argentinos con coma decimal y, opcionalmente, punto de miles
 * (RF-03): "1.234,56", "1234,56", "300.000", "15000".
 */
const AMOUNT_PATTERN = /^(\d{1,3}(?:\.\d{3})+|\d+)(?:,(\d+))?$/

export type ParsedAmount = { ok: true; value: number } | { ok: false; error: string }

export function parseAmount(input: string): ParsedAmount {
  const text = input.trim()
  if (text === '') {
    return { ok: false, error: 'El monto es obligatorio.' }
  }

  const match = AMOUNT_PATTERN.exec(text)
  if (!match) {
    return { ok: false, error: 'Ingresá el monto con coma decimal, por ejemplo 1.234,56.' }
  }

  const [, integerPart, decimals = ''] = match
  if (decimals.length > 2) {
    return { ok: false, error: 'El monto admite hasta 2 decimales.' }
  }

  const value = Number(`${integerPart.replaceAll('.', '')}.${decimals || '0'}`)
  if (value <= 0) {
    return { ok: false, error: 'El monto debe ser mayor a 0.' }
  }

  return { ok: true, value }
}

const currency = new Intl.NumberFormat('es-AR', {
  style: 'currency',
  currency: 'ARS',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

/** 1234.56 → "$ 1.234,56" */
export function formatAmount(value: number): string {
  return currency.format(value)
}

const inputNumber = new Intl.NumberFormat('es-AR', { maximumFractionDigits: 2 })

/** 1234.56 → "1.234,56": el monto como se escribe en el formulario. */
export function formatAmountInput(value: number): string {
  return inputNumber.format(value)
}
