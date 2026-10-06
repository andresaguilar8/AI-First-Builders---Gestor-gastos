using Microsoft.EntityFrameworkCore;

namespace GestorGastos.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
