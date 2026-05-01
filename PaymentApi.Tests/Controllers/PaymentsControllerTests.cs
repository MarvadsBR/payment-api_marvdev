using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentApi.Controllers;
using PaymentApi.DTOs;
using PaymentApi.Models;
using PaymentApi.Services;

namespace PaymentApi.Tests.Controllers;

/// <summary>
/// Testes unitários do PaymentsController.
///
/// FERRAMENTA: xUnit + Moq
/// ────────────────────────
/// O controller depende de IPaymentService (interface).
/// Com Moq criamos um "dublê" (mock) do service que retorna
/// valores controlados. Isso permite testar SOMENTE a lógica
/// do controller (códigos HTTP, roteamento, respostas) sem
/// depender de banco de dados ou do service real.
///
/// FLUXO PADRÃO DE UM TESTE COM MOQ:
///   1. Criar o mock:  var mock = new Mock<IPaymentService>();
///   2. Configurar:    mock.Setup(s => s.MetodoX(...)).ReturnsAsync(valor);
///   3. Injetar:       new PaymentsController(mock.Object)
///   4. Agir:          var result = await controller.Action(...)
///   5. Verificar:     Assert no tipo e conteúdo da resposta
/// </summary>
public class PaymentsControllerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS PRIVADOS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// PaymentResponseDto com valores padrão para reutilização nos testes.
    /// </summary>
    private static PaymentResponseDto SampleResponse(Guid? id = null) => new()
    {
        Id          = id ?? Guid.NewGuid(),
        Amount      = 150m,
        Currency    = "BRL",
        Status      = "Pending",
        Method      = "Pix",
        Description = "Sample",
        CreatedAt   = DateTime.UtcNow,
        UpdatedAt   = DateTime.UtcNow
    };

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/payments
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk_WithListOfPayments()
    {
        // ARRANGE
        var mock = new Mock<IPaymentService>();

        // Setup: quando GetAllAsync for chamado com qualquer string? (It.IsAny),
        // retornar uma lista com 2 itens.
        mock.Setup(s => s.GetAllAsync(It.IsAny<string?>()))
            .ReturnsAsync(new[] { SampleResponse(), SampleResponse() });

        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.GetAll(status: null);

        // ASSERT
        // Verificamos que o resultado é um OkObjectResult (HTTP 200)
        var ok = Assert.IsType<OkObjectResult>(actionResult);

        // E que o valor contém 2 itens
        var items = Assert.IsAssignableFrom<IEnumerable<PaymentResponseDto>>(ok.Value);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetAll_PassesStatusFilterToService()
    {
        // ARRANGE
        var mock = new Mock<IPaymentService>();
        mock.Setup(s => s.GetAllAsync("Pending"))
            .ReturnsAsync(new[] { SampleResponse() });

        var controller = new PaymentsController(mock.Object);

        // ACT
        await controller.GetAll(status: "Pending");

        // ASSERT — verificamos que o service foi chamado exatamente 1 vez
        // com o valor "Pending". Garante que o controller repassa o filtro.
        mock.Verify(s => s.GetAllAsync("Pending"), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/payments/{id}
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithPayment()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var mock = new Mock<IPaymentService>();
        mock.Setup(s => s.GetByIdAsync(id))
            .ReturnsAsync(SampleResponse(id));

        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.GetById(id);

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<PaymentResponseDto>(ok.Value);
        Assert.Equal(id, dto.Id);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // ARRANGE
        var mock = new Mock<IPaymentService>();

        // Service retorna null → controller deve retornar 404
        mock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((PaymentResponseDto?)null);

        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.GetById(Guid.NewGuid());

        // ASSERT
        Assert.IsType<NotFoundResult>(actionResult);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/payments
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        // ARRANGE
        var createdId = Guid.NewGuid();
        var mock = new Mock<IPaymentService>();
        mock.Setup(s => s.CreateAsync(It.IsAny<CreatePaymentDto>()))
            .ReturnsAsync(SampleResponse(createdId));

        var controller = new PaymentsController(mock.Object);

        var dto = new CreatePaymentDto
        {
            Amount      = 150m,
            Currency    = "BRL",
            Method      = PaymentMethod.Pix,
            Description = "Test"
        };

        // ACT
        var actionResult = await controller.Create(dto);

        // ASSERT
        // POST bem-sucedido deve retornar 201 CreatedAtAction, não 200 OK
        var created = Assert.IsType<CreatedAtActionResult>(actionResult);
        Assert.Equal(201, created.StatusCode);

        var response = Assert.IsType<PaymentResponseDto>(created.Value);
        Assert.Equal(createdId, response.Id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATCH /api/payments/{id}/status
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ExistingId_ReturnsOkWithUpdatedPayment()
    {
        // ARRANGE
        var id = Guid.NewGuid();
        var updatedDto = SampleResponse(id);
        updatedDto.Status = "Completed";

        var mock = new Mock<IPaymentService>();
        mock.Setup(s => s.UpdateStatusAsync(id, It.IsAny<UpdatePaymentStatusDto>()))
            .ReturnsAsync(updatedDto);

        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.UpdateStatus(id, new UpdatePaymentStatusDto { Status = PaymentStatus.Completed });

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<PaymentResponseDto>(ok.Value);
        Assert.Equal("Completed", response.Status);
    }

    [Fact]
    public async Task UpdateStatus_NonExistingId_ReturnsNotFound()
    {
        // ARRANGE
        var mock = new Mock<IPaymentService>();
        mock.Setup(s => s.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<UpdatePaymentStatusDto>()))
            .ReturnsAsync((PaymentResponseDto?)null);

        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.UpdateStatus(Guid.NewGuid(), new UpdatePaymentStatusDto { Status = PaymentStatus.Completed });

        // ASSERT
        Assert.IsType<NotFoundResult>(actionResult);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE /api/payments/{id}
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_PendingPayment_ReturnsNoContent()
    {
        // ARRANGE
        var mock = new Mock<IPaymentService>();
        mock.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
            .ReturnsAsync(DeleteResult.Success);

        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.Delete(Guid.NewGuid());

        // ASSERT — deleção bem-sucedida deve retornar 204 No Content (sem corpo)
        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        // ARRANGE
        var mock = new Mock<IPaymentService>();
        mock.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
            .ReturnsAsync(DeleteResult.NotFound);

        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.Delete(Guid.NewGuid());

        // ASSERT
        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task Delete_NonPendingPayment_ReturnsConflict()
    {
        // ARRANGE
        var mock = new Mock<IPaymentService>();
        mock.Setup(s => s.DeleteAsync(It.IsAny<Guid>()))
            .ReturnsAsync(DeleteResult.NotAllowed);

        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.Delete(Guid.NewGuid());

        // ASSERT — tentar deletar um pagamento não-Pending deve retornar 409 Conflict
        var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal(409, conflict.StatusCode);
    }
}
