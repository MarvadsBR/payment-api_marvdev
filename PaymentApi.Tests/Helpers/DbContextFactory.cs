using Microsoft.EntityFrameworkCore;
using PaymentApi.Data;

namespace PaymentApi.Tests.Helpers;

/// <summary>
/// Fábrica de AppDbContext usando banco em memória (InMemory provider).
///
/// POR QUE InMemory em vez de Moq para o DbContext?
/// ─────────────────────────────────────────────────
/// Mockar DbContext diretamente com Moq é muito trabalhoso e frágil:
/// seria necessário mockar DbSet, IQueryable, FindAsync, etc.
/// O provider InMemory do EF Core cria um banco real em RAM, permitindo
/// testar queries LINQ, Add, Remove e SaveChanges com código real.
///
/// Cada teste recebe um nome de banco ÚNICO para garantir isolamento total.
/// Sem isolamento, dados de um teste vazariam para o próximo.
/// </summary>
public static class DbContextFactory
{
    /// <summary>
    /// Cria um AppDbContext apontando para um banco em memória isolado.
    /// </summary>
    /// <param name="dbName">
    /// Nome único do banco. Passe nameof(MeuTeste) ou Guid.NewGuid().ToString()
    /// para garantir que cada teste trabalha com um banco limpo.
    /// </param>
    public static AppDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new AppDbContext(options);
    }
}
