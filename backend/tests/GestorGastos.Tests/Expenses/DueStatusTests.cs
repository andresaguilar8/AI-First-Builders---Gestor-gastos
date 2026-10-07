using GestorGastos.Expenses;
using GestorGastos.Time;

namespace GestorGastos.Tests.Expenses;

public class DueStatusTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    private static MonthlyExpense Pending(DateOnly? dueDate) =>
        new(1, new Period(2026, 10), ExpenseKind.OneOff, "Luz", null, 1_000m, dueDate, PaidOn: null);

    [Theory]
    [InlineData("2026-10-09", DueStatus.UpToDate)] // AC-43: faltan 4 días
    [InlineData("2026-11-20", DueStatus.UpToDate)]
    [InlineData("2026-10-08", DueStatus.DueSoon)] // AC-45: faltan 3 días
    [InlineData("2026-10-06", DueStatus.DueSoon)] // AC-45: falta 1 día
    [InlineData("2026-10-05", DueStatus.DueToday)] // AC-46
    [InlineData("2026-10-04", DueStatus.Overdue)] // AC-47
    [InlineData("2026-09-01", DueStatus.Overdue)]
    public void Calcula_el_estado_de_un_pendiente_segun_los_dias_al_vencimiento(string dueDate, DueStatus expected)
    {
        Assert.Equal(expected, DueStatuses.Of(Pending(DateOnly.Parse(dueDate)), Today));
    }

    [Fact]
    public void Un_pendiente_sin_vencimiento_esta_al_dia()
    {
        // AC-44
        Assert.Equal(DueStatus.UpToDate, DueStatuses.Of(Pending(dueDate: null), Today));
    }

    [Theory]
    [InlineData("2026-10-01")]
    [InlineData("2026-10-05")]
    [InlineData(null)]
    public void Un_gasto_pagado_esta_pagado_aunque_haya_vencido(string? dueDate)
    {
        var paid = Pending(dueDate is null ? null : DateOnly.Parse(dueDate)) with { PaidOn = new DateOnly(2026, 10, 2) };

        Assert.Equal(DueStatus.Paid, DueStatuses.Of(paid, Today));
    }

    [Fact]
    public void Cuenta_los_dias_aunque_el_vencimiento_sea_de_otro_mes()
    {
        var lastDayOfMonth = new DateOnly(2026, 10, 31);

        Assert.Equal(DueStatus.DueSoon, DueStatuses.Of(Pending(new DateOnly(2026, 11, 2)), lastDayOfMonth));
    }
}
