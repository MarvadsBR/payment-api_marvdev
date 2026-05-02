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

    private static PagedResponseDto<PaymentResponseDto> SamplePaged(params PaymentResponseDto[] items) => new()
    {
        Page       = 1,
        PageSize   = 10,
        TotalCount = items.Length,
        Data       = items
    };

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/payments
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResponse()
    {
        // ARRANGE
        var mock = new Mock<IPaymentService>();
        mock.Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(SamplePaged(SampleResponse(), SampleResponse()));

        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.GetAll(status: null, page: 1, pageSize: 10);

        // ASSERT
        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var paged = Assert.IsType<PagedResponseDto<PaymentResponseDto>>(ok.Value);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(2, paged.Data.Count());
    }

    [Fact]
    public async Task GetAll_InvalidPage_ReturnsBadRequest()
    {
        // ARRANGE — page=0 deve ser rejeitado pelo controller antes de chamar o service
        var mock = new Mock<IPaymentService>();
        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.GetAll(status: null, page: 0, pageSize: 10);

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(actionResult);
        mock.Verify(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_InvalidPageSize_ReturnsBadRequest()
    {
        // ARRANGE — pageSize=200 excede o limite de 100
        var mock = new Mock<IPaymentService>();
        var controller = new PaymentsController(mock.Object);

        // ACT
        var actionResult = await controller.GetAll(status: null, page: 1, pageSize: 200);

        // ASSERT
        Assert.IsType<BadRequestObjectResult>(actionResult);
        mock.Verify(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
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
