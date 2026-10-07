import { describe, expect, it } from 'vitest'
import { formatAmount, formatAmountInput, parseAmount } from './money'

describe('parseAmount', () => {
  it.each([
    ['1.234,56', 1234.56], // AC-06
    ['1234,56', 1234.56],
    ['300.000', 300000],
    ['15000', 15000],
    ['0,5', 0.5],
    ['1.000.000,10', 1000000.1],
    ['  250  ', 250],
  ])('acepta %s', (input, expected) => {
    expect(parseAmount(input)).toEqual({ ok: true, value: expected })
  })

  it.each([
    ['0', 'mayor a 0'], // AC-05
    ['0,00', 'mayor a 0'],
    ['-100', 'coma decimal'], // AC-05
    ['10,999', 'hasta 2 decimales'], // AC-05
    ['', 'obligatorio'],
    ['1.5', 'coma decimal'],
    ['12.34.567', 'coma decimal'],
    ['1,234.56', 'coma decimal'],
    ['abc', 'coma decimal'],
  ])('rechaza %s', (input, message) => {
    const result = parseAmount(input)
    expect(result.ok).toBe(false)
    expect(!result.ok && result.error).toContain(message)
  })
})

describe('formatAmount', () => {
  it('usa el formato argentino', () => {
    // Intl separa el símbolo con un espacio no separable.
    expect(formatAmount(1234.56).replace(/\s/g, ' ')).toBe('$ 1.234,56')
    expect(formatAmount(300000).replace(/\s/g, ' ')).toBe('$ 300.000,00')
  })
})

describe('formatAmountInput', () => {
  it.each([
    [1234.56, '1.234,56'],
    [15000, '15.000'],
    [0.5, '0,5'],
  ])('muestra %s como %s y se puede volver a leer', (value, text) => {
    expect(formatAmountInput(value)).toBe(text)
    expect(parseAmount(text)).toEqual({ ok: true, value })
  })
})
