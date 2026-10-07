using GestorGastos.Expenses;
using GestorGastos.Time;

namespace GestorGastos.Tests.Expenses;

public class ExpenseProjectionTests
{
    private static readonly Period October2026 = new(2026, 10);

    private static Expense Recurring(Period start, DateOnly? dueDate = null, decimal amount = 30_000m) => new()
    {
        Id = 1,
        Name = "Alquiler",
        Description = "Depto",
        Amount = amount,
        DueDate = dueDate,
        Kind = ExpenseKind.Recurring,
        StartPeriod = start,
    };

    private static Expense OneOff(Period period, DateOnly? dueDate = null) => new()
    {
        Id = 2,
        Name = "Regalo",
        Amount = 15_000m,
        DueDate = dueDate,
        Kind = ExpenseKind.OneOff,
        StartPeriod = period,
        EndPeriod = period,
    };

    [Fact]
    public void Un_recurrente_aparece_en_el_mes_siguiente_sin_registro_propio()
    {
        // AC-09
        var expense = Recurring(October2026);

        var november = ExpenseProjection.Project(expense, ownMonth: null, October2026.Next());

        Assert.NotNull(november);
        Assert.Equal(new Period(2026, 11), november.Period);
        Assert.Equal("Alquiler", november.Name);
        Assert.Equal("Depto", november.Description);
        Assert.Equal(30_000m, november.Amount);
        Assert.Equal(ExpenseKind.Recurring, november.Kind);
        Assert.Null(november.PaidOn);
    }

    [Fact]
    public void Un_recurrente_activo_aparece_en_meses_lejanos()
    {
        var expense = Recurring(October2026);

        Assert.True(ExpenseProjection.AppearsIn(expense, new Period(2036, 10)));
    }

    [Fact]
    public void Un_recurrente_no_aparece_antes_de_su_mes_de_alta()
    {
        var expense = Recurring(October2026);

        Assert.Null(ExpenseProjection.Project(expense, null, October2026.Previous()));
    }

    [Fact]
    public void Un_recurrente_con_fin_aparece_hasta_ese_mes_inclusive()
    {
        var expense = Recurring(new Period(2026, 8));
        expense.EndPeriod = October2026;

        Assert.True(ExpenseProjection.AppearsIn(expense, October2026));
        Assert.False(ExpenseProjection.AppearsIn(expense, October2026.Next()));
    }

    [Fact]
    public void Un_puntual_aparece_solo_en_su_mes()
    {
        // AC-13
        var expense = OneOff(October2026);

        Assert.NotNull(ExpenseProjection.Project(expense, null, October2026));
        Assert.Null(ExpenseProjection.Project(expense, null, October2026.Next()));
        Assert.Null(ExpenseProjection.Project(expense, null, October2026.Previous()));
    }

    [Fact]
    public void Un_puntual_aparece_solo_en_su_mes_aunque_le_falte_el_fin()
    {
        var expense = OneOff(October2026);
        expense.EndPeriod = null;

        Assert.False(ExpenseProjection.AppearsIn(expense, October2026.Next()));
    }

    [Fact]
    public void El_vencimiento_usa_el_ultimo_dia_cuando_el_mes_no_tiene_ese_dia()
    {
        // AC-10
        var expense = Recurring(new Period(2027, 1), new DateOnly(2027, 1, 30));

        Assert.Equal(new DateOnly(2027, 1, 30), ExpenseProjection.DueDateIn(expense, new Period(2027, 1)));
        Assert.Equal(new DateOnly(2027, 2, 28), ExpenseProjection.DueDateIn(expense, new Period(2027, 2)));
        Assert.Equal(new DateOnly(2027, 3, 30), ExpenseProjection.DueDateIn(expense, new Period(2027, 3)));
    }

    [Fact]
    public void El_vencimiento_del_mes_de_alta_puede_caer_en_el_mes_siguiente()
    {
        // AC-11
        var expense = Recurring(October2026, new DateOnly(2026, 11, 5));

        Assert.Equal(new DateOnly(2026, 11, 5), ExpenseProjection.DueDateIn(expense, October2026));
        Assert.Equal(new DateOnly(2026, 11, 5), ExpenseProjection.DueDateIn(expense, new Period(2026, 11)));
        Assert.Equal(new DateOnly(2026, 12, 5), ExpenseProjection.DueDateIn(expense, new Period(2026, 12)));
    }

    [Fact]
    public void Sin_vencimiento_sigue_sin_vencimiento_en_los_meses_proyectados()
    {
        var expense = Recurring(October2026, dueDate: null);

        Assert.Null(ExpenseProjection.Project(expense, null, new Period(2027, 3))!.DueDate);
    }

    [Fact]
    public void Un_puntual_con_vencimiento_en_el_mes_siguiente_lo_conserva()
    {
        var expense = OneOff(October2026, new DateOnly(2026, 11, 5));

        Assert.Equal(new DateOnly(2026, 11, 5), ExpenseProjection.Project(expense, null, October2026)!.DueDate);
    }

    [Fact]
    public void La_instancia_propia_del_mes_reemplaza_los_valores_proyectados()
    {
        var expense = Recurring(October2026, new DateOnly(2026, 10, 10));
        var november = new Period(2026, 11);
        var ownMonth = new ExpenseMonth
        {
            ExpenseId = expense.Id,
            Period = november,
            Name = "Alquiler + expensas",
            Description = null,
            Amount = 32_000m,
            DueDate = new DateOnly(2026, 11, 12),
            PaidOn = new DateOnly(2026, 11, 8),
        };

        var projected = ExpenseProjection.Project(expense, ownMonth, november)!;

        Assert.Equal("Alquiler + expensas", projected.Name);
        Assert.Null(projected.Description);
        Assert.Equal(32_000m, projected.Amount);
        Assert.Equal(new DateOnly(2026, 11, 12), projected.DueDate);
        Assert.Equal(new DateOnly(2026, 11, 8), projected.PaidOn);
    }

    [Fact]
    public void Rechaza_una_instancia_de_otro_mes()
    {
        var expense = Recurring(October2026);
        var ownMonth = new ExpenseMonth { ExpenseId = expense.Id, Period = October2026, Name = "x", Amount = 1m };

        Assert.Throws<ArgumentException>(() => ExpenseProjection.Project(expense, ownMonth, October2026.Next()));
    }
}
