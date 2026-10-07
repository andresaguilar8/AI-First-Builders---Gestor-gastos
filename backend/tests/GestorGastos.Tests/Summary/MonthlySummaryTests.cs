using GestorGastos.Expenses;
using GestorGastos.Summary;
using GestorGastos.Time;

namespace GestorGastos.Tests.Summary;

public class MonthlySummaryTests
{
    private static readonly Period September = new(2026, 9);

    private static readonly Dictionary<int, string> Names = new() { [1] = "Vivienda", [2] = "Servicios" };

    private static MonthlyExpense Expense(decimal amount, int? categoryId, bool paid = false) =>
        new(1, September, ExpenseKind.OneOff, "Gasto", null, amount, null,
            PaidOn: paid ? new DateOnly(2026, 9, 3) : null, CategoryId: categoryId);

    [Fact]
    public void Suma_pagados_y_pendientes_por_categoria()
    {
        // AC-32
        var summary = MonthlySummary.From(September, [
            Expense(200_000m, 1, paid: true),
            Expense(100_000m, 1),
            Expense(100_000m, 2),
        ], Names);

        Assert.Equal(
            [(new CategoryRef(1, "Vivienda"), 300_000m, 2), (new CategoryRef(2, "Servicios"), 100_000m, 1)],
            summary.ByCategory.Select(c => (c.Category, c.Total, c.Count)));
    }

    [Fact]
    public void Agrupa_los_gastos_sin_categoria()
    {
        // AC-33
        var summary = MonthlySummary.From(September, [Expense(50_000m, categoryId: null)], Names);

        var group = Assert.Single(summary.ByCategory);
        Assert.Null(group.Category);
        Assert.Equal(50_000m, group.Total);
    }

    [Fact]
    public void Una_categoria_que_ya_no_existe_cuenta_como_sin_categoria()
    {
        var summary = MonthlySummary.From(September, [Expense(10m, 99), Expense(5m, null)], Names);

        var group = Assert.Single(summary.ByCategory);
        Assert.Null(group.Category);
        Assert.Equal(15m, group.Total);
    }

    [Fact]
    public void Calcula_el_total_del_mes_y_el_pendiente()
    {
        // AC-34
        var summary = MonthlySummary.From(September, [
            Expense(300_000m, 1, paid: true),
            Expense(60_000m, 2),
            Expense(40_000m, null),
        ], Names);

        Assert.Equal(400_000m, summary.Total);
        Assert.Equal(100_000m, summary.Pending);
    }

    [Fact]
    public void Ordena_de_mayor_a_menor_y_en_empate_deja_sin_categoria_al_final()
    {
        var summary = MonthlySummary.From(September, [
            Expense(100m, null),
            Expense(100m, 2),
            Expense(500m, 1),
        ], Names);

        Assert.Equal(["Vivienda", "Servicios", null], summary.ByCategory.Select(c => c.Category?.Name));
    }

    [Fact]
    public void Un_mes_sin_gastos_da_todo_en_cero()
    {
        var summary = MonthlySummary.From(September, [], Names);

        Assert.Equal((0m, 0m), (summary.Total, summary.Pending));
        Assert.Empty(summary.ByCategory);
    }
}
