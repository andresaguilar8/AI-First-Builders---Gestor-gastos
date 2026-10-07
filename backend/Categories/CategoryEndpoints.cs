using System.Globalization;
using GestorGastos.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GestorGastos.Categories;

public static class CategoryEndpoints
{
    private const string DuplicateName = "Ya existe una categoría con ese nombre.";

    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var categories = app.MapGroup("/api/categories");

        categories.MapGet("/", async (AppDbContext db, CancellationToken cancellationToken) =>
        {
            var all = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
            return TypedResults.Ok(all
                .OrderBy(c => c.Name, StringComparer.Create(new("es-AR"), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace))
                .Select(CategoryResponse.From)
                .ToList());
        });

        // RF-18
        categories.MapPost("/", async (CategoryRequest request, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var (name, errors) = await ValidateAsync(request, db, categoryId: null, cancellationToken);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var category = new Category { Name = name!, NormalizedName = CategoryNames.Normalize(name!) };
            db.Categories.Add(category);
            if (!await TrySaveAsync(db, cancellationToken))
            {
                return DuplicateNameProblem();
            }

            return Results.Created($"/api/categories/{category.Id}", CategoryResponse.From(category));
        });

        // RF-20: el nombre nuevo se ve en todos los meses porque los gastos guardan solo el id.
        categories.MapPut("/{id:int}", async (int id, CategoryRequest request, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var category = await db.Categories.FindAsync([id], cancellationToken);
            if (category is null)
            {
                return Results.NotFound();
            }

            var (name, errors) = await ValidateAsync(request, db, id, cancellationToken);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            category.Name = name!;
            category.NormalizedName = CategoryNames.Normalize(name!);
            if (!await TrySaveAsync(db, cancellationToken))
            {
                return DuplicateNameProblem();
            }

            return Results.Ok(CategoryResponse.From(category));
        });

        // RF-21
        categories.MapDelete("/{id:int}", async (int id, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var deleted = await db.Categories.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken);
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        return app;
    }

    /// <summary>El nombre limpio y los errores por campo (RF-19).</summary>
    private static async Task<(string? Name, Dictionary<string, string[]> Errors)> ValidateAsync(
        CategoryRequest request, AppDbContext db, int? categoryId, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var name = request.Name is null ? "" : CategoryNames.Clean(request.Name);

        if (name.Length == 0)
        {
            errors["name"] = ["El nombre es obligatorio."];
        }
        else if (name.Length > CategoryNames.MaxLength)
        {
            errors["name"] = [$"El nombre no puede superar los {CategoryNames.MaxLength} caracteres."];
        }
        else if (CategoryNames.IsReserved(name))
        {
            errors["name"] = [$"\"{CategoryNames.Uncategorized}\" es un nombre reservado."];
        }
        else
        {
            var normalized = CategoryNames.Normalize(name);
            var taken = await db.Categories.AnyAsync(c => c.NormalizedName == normalized && c.Id != categoryId, cancellationToken);
            if (taken)
            {
                errors["name"] = [DuplicateName];
            }
        }

        return (name, errors);
    }

    /// <summary>
    /// Guarda los cambios. Devuelve false si otro pedido ocupó el nombre entre la
    /// validación y el guardado (lo detecta el índice único).
    /// </summary>
    private static async Task<bool> TrySaveAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    private static IResult DuplicateNameProblem() =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = [DuplicateName] });
}
