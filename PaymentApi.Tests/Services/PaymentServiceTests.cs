using PaymentApi.DTOs;
using PaymentApi.Models;
using PaymentApi.Services;
using PaymentApi.Tests.Helpers;

namespace PaymentApi.Tests.Services;

/// <summary>
/// Testes unitários do PaymentService.
///
/// FERRAMENTA: xUnit + EF Core InMemory
/// ─────────────────────────────────────
/// Testamos o PaymentService diretamente com um AppDbContext real,
/// porém usando banco em memória. Isso verifica que:
///   • As queries LINQ estão corretas
///   • A lógica de negócio funciona (ex: só Pending pode ser deletado)
///   • As conversões DTO ↔ entidade estão corretas
///
/// CONVENÇÃO DE NOME: MetodoTestado_Cenario_ResultadoEsperado
/// Exemplo: GetAllAsync_WithStatusFilter_ReturnsOnlyMatchingPayments
/// Essa convenção torna o nome do teste autoexplicativo no relatório.
/// </summary>
public class PaymentServiceTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS PRIVADOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cria um CreatePaymentDto com valores padrão válidos.
    /// Centralizar a criação evita repetição e facilita manutenção.
    /// </summary>
    private static CreatePaymentDto DefaultCreateDto(
        decimal amount = 100m,
        string currency = "BRL",
        PaymentMethod method = PaymentMethod.Pix,
        string description = "Test payment") => new()
    {
        Amount = amount,
        Currency = currency,
        Method = method,
        Description = description
    };

    // ─────────────────────────────────────────────────────────────────────────
    // GetAllAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsAllPayments()
    {
        // ARRANGE ─────────────────────────────────────────────────────────────
        // Criamos um banco isolado para este teste específico.
        // nameof() garante um nome único sem precisar de Guid.
        using var db = DbContextFactory.Create(nameof(GetAllAsync_NoFilter_ReturnsAllPayments));
        var service = new PaymentService(db);

        // Populamos o banco com 2 pagamentos em estados diferentes.
        db.Payments.AddRange(
            new Payment { Description = "P1", Status = PaymentStatus.Pending,   Method = PaymentMethod.Pix,        Amount = 10, Currency = "BRL" },
            new Payment { Description = "P2", Status = PaymentStatus.Completed, Method = PaymentMethod.CreditCard,  Amount = 20, Currency = "USD" }
        );
        await db.SaveChangesAsync();

        // ACT ─────────────────────────────────────────────────────────────────
        var result = await service.GetAllAsync(status: null, page: 1, pageSize: 10);

        // ASSERT ──────────────────────────────────────────────────────────────
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithValidStatusFilter_ReturnsOnlyMatchingPayments()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(GetAllAsync_WithValidStatusFilter_ReturnsOnlyMatchingPayments));
        var service = new PaymentService(db);

        db.Payments.AddRange(
            new Payment { Description = "Pending 1",   Status = PaymentStatus.Pending,   Method = PaymentMethod.Pix,       Amount = 10, Currency = "BRL" },
            new Payment { Description = "Pending 2",   Status = PaymentStatus.Pending,   Method = PaymentMethod.Pix,       Amount = 20, Currency = "BRL" },
            new Payment { Description = "Completed 1", Status = PaymentStatus.Completed, Method = PaymentMethod.CreditCard, Amount = 30, Currency = "BRL" }
        );
        await db.SaveChangesAsync();

        // ACT
        var result = await service.GetAllAsync(status: "Pending", page: 1, pageSize: 10);

        // ASSERT
        // Apenas os 2 pagamentos Pending devem ser retornados.
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Data, r => Assert.Equal("Pending", r.Status));
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidStatusFilter_ReturnsAllPayments()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(GetAllAsync_WithInvalidStatusFilter_ReturnsAllPayments));
        var service = new PaymentService(db);

        db.Payments.AddRange(
            new Payment { Description = "P1", Status = PaymentStatus.Pending,   Method = PaymentMethod.Pix, Amount = 10, Currency = "BRL" },
            new Payment { Description = "P2", Status = PaymentStatus.Completed, Method = PaymentMethod.Pix, Amount = 20, Currency = "BRL" }
        );
        await db.SaveChangesAsync();

        // ACT
        // "InvalidStatus" não existe no enum → Enum.TryParse falha → sem filtro.
        var result = await service.GetAllAsync(status: "InvalidStatus", page: 1, pageSize: 10);

        // ASSERT
        // Comportamento esperado: retorna tudo quando o filtro é inválido.
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_FilterIsCaseInsensitive()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(GetAllAsync_FilterIsCaseInsensitive));
        var service = new PaymentService(db);

        db.Payments.Add(new Payment { Description = "P1", Status = PaymentStatus.Completed, Method = PaymentMethod.Pix, Amount = 10, Currency = "BRL" });
        await db.SaveChangesAsync();

        // ACT — "completed" em minúsculas deve funcionar igual a "Completed"
        var result = await service.GetAllAsync(status: "completed", page: 1, pageSize: 10);

        // ASSERT
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Data);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetByIdAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsPaymentDto()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(GetByIdAsync_ExistingId_ReturnsPaymentDto));
        var service = new PaymentService(db);

        var payment = new Payment
        {
            Description = "Checkout payment",
            Amount = 250m,
            Currency = "BRL",
            Method = PaymentMethod.CreditCard,
            Status = PaymentStatus.Pending
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        // ACT
        var result = await service.GetByIdAsync(payment.Id);

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal(payment.Id, result.Id);
        Assert.Equal(250m, result.Amount);
        Assert.Equal("BRL", result.Currency);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(GetByIdAsync_NonExistingId_ReturnsNull));
        var service = new PaymentService(db);

        // ACT — passamos um Guid aleatório que não existe no banco
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // ASSERT
        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedPaymentWithPendingStatus()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(CreateAsync_ValidDto_ReturnsCreatedPaymentWithPendingStatus));
        var service = new PaymentService(db);

        var dto = DefaultCreateDto(amount: 99.99m, method: PaymentMethod.Pix, description: "New order");

        // ACT
        var result = await service.CreateAsync(dto);

        // ASSERT
        Assert.NotEqual(Guid.Empty, result.Id);     // Id foi gerado
        Assert.Equal(99.99m, result.Amount);
        Assert.Equal("Pending", result.Status);      // Status inicial sempre Pending
        Assert.Equal("Pix", result.Method);
        Assert.Equal("New order", result.Description);
    }

    [Fact]
    public async Task CreateAsync_CurrencyIsStoredUppercase()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(CreateAsync_CurrencyIsStoredUppercase));
        var service = new PaymentService(db);

        // Passamos "brl" em minúsculas intencionalmente
        var dto = DefaultCreateDto(currency: "brl");

        // ACT
        var result = await service.CreateAsync(dto);

        // ASSERT — o service deve normalizar para "BRL"
        Assert.Equal("BRL", result.Currency);
    }

    [Fact]
    public async Task CreateAsync_PaymentIsPersistedInDatabase()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(CreateAsync_PaymentIsPersistedInDatabase));
        var service = new PaymentService(db);

        // ACT
        var result = await service.CreateAsync(DefaultCreateDto());

        // ASSERT — verificamos diretamente no banco que o registro foi salvo
        var saved = await db.Payments.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal(result.Amount, saved.Amount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateStatusAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ExistingId_UpdatesStatusAndReturnsDto()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(UpdateStatusAsync_ExistingId_UpdatesStatusAndReturnsDto));
        var service = new PaymentService(db);

        var payment = new Payment { Description = "P", Amount = 10, Currency = "BRL", Method = PaymentMethod.Pix };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var updateDto = new UpdatePaymentStatusDto { Status = PaymentStatus.Completed };

        // ACT
        var result = await service.UpdateStatusAsync(payment.Id, updateDto);

        // ASSERT
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);

        // Verificação dupla: o banco também foi atualizado
        var updated = await db.Payments.FindAsync(payment.Id);
        Assert.Equal(PaymentStatus.Completed, updated!.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistingId_ReturnsNull()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(UpdateStatusAsync_NonExistingId_ReturnsNull));
        var service = new PaymentService(db);

        var updateDto = new UpdatePaymentStatusDto { Status = PaymentStatus.Completed };

        // ACT
        var result = await service.UpdateStatusAsync(Guid.NewGuid(), updateDto);

        // ASSERT
        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DeleteAsync
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_PendingPayment_ReturnsSuccess()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(DeleteAsync_PendingPayment_ReturnsSuccess));
        var service = new PaymentService(db);

        var payment = new Payment
        {
            Description = "To delete",
            Amount = 10,
            Currency = "BRL",
            Method = PaymentMethod.Pix,
            Status = PaymentStatus.Pending   // <-- único estado que permite deleção
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        // ACT
        var result = await service.DeleteAsync(payment.Id);

        // ASSERT
        Assert.Equal(DeleteResult.Success, result);

        // Verificamos que o registro foi removido do banco
        var deleted = await db.Payments.FindAsync(payment.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsNotFound()
    {
        // ARRANGE
        using var db = DbContextFactory.Create(nameof(DeleteAsync_NonExistingId_ReturnsNotFound));
        var service = new PaymentService(db);

        // ACT
        var result = await service.DeleteAsync(Guid.NewGuid());

        // ASSERT
        Assert.Equal(DeleteResult.NotFound, result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAllAsync — paginação
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
    {
        // ARRANGE — 5 pagamentos; pedimos page=2, pageSize=2 → esperamos 2 itens
        using var db = DbContextFactory.Create(nameof(GetAllAsync_Pagination_ReturnsCorrectPage));
        var service = new PaymentService(db);

        for (int i = 0; i < 5; i++)
            db.Payments.Add(new Payment { Description = $"P{i}", Amount = i + 1, Currency = "BRL", Method = PaymentMethod.Pix });
        await db.SaveChangesAsync();

        // ACT
        var result = await service.GetAllAsync(status: null, page: 2, pageSize: 2);

        // ASSERT
        Assert.Equal(5, result.TotalCount);   // total real no banco
        Assert.Equal(2, result.Data.Count()); // apenas 2 na página
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);   // ceil(5/2)
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task GetAllAsync_LastPage_HasNextPageFalse()
    {
        // ARRANGE — 3 pagamentos; page=2, pageSize=2 → página final com 1 item
        using var db = DbContextFactory.Create(nameof(GetAllAsync_LastPage_HasNextPageFalse));
        var service = new PaymentService(db);

        for (int i = 0; i < 3; i++)
            db.Payments.Add(new Payment { Description = $"P{i}", Amount = i + 1, Currency = "BRL", Method = PaymentMethod.Pix });
        await db.SaveChangesAsync();

        // ACT
        var result = await service.GetAllAsync(status: null, page: 2, pageSize: 2);

        // ASSERT
        Assert.Single(result.Data);  // apenas 1 sobrou na última página
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    /// <summary>
    /// Testa que pagamentos em estados diferentes de Pending não podem ser deletados.
    /// Usamos Theory + InlineData para rodar o mesmo teste com múltiplos cenários
    /// sem duplicar código — cada linha [InlineData] vira um caso separado no relatório.
    /// </summary>
    [Theory]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Refunded)]
    public async Task DeleteAsync_NonPendingPayment_ReturnsNotAllowed(PaymentStatus status)
    {
        // ARRANGE
        // O nome do banco inclui o status para garantir banco isolado por caso
        using var db = DbContextFactory.Create($"{nameof(DeleteAsync_NonPendingPayment_ReturnsNotAllowed)}_{status}");
        var service = new PaymentService(db);

        var payment = new Payment
        {
            Description = "Cannot delete",
            Amount = 10,
            Currency = "BRL",
            Method = PaymentMethod.Pix,
            Status = status
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        // ACT
        var result = await service.DeleteAsync(payment.Id);

        // ASSERT
        Assert.Equal(DeleteResult.NotAllowed, result);
    }
}
