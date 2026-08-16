using Microsoft.EntityFrameworkCore;

namespace Abc.JogoDoVelho.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);

