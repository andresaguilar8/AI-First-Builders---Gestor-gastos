namespace GestorGastos.Categories;

public sealed record CategoryResponse(int Id, string Name)
{
    public static CategoryResponse From(Category category) => new(category.Id, category.Name);
}
