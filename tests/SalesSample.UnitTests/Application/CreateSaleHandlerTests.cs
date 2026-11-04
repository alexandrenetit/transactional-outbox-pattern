using Bogus; // dados falsos realistas
using Moq; // dublês
using SalesSample.Application.Sales.Commands.CreateSale;
using SalesSample.Domain.Entities;
using SalesSample.Domain.Exceptions;
using SalesSample.Domain.Repositories;
using SalesSample.Domain.Services;
using SalesSample.Domain.ValueObjects;
using Shouldly; // asserções
using Xunit; // [Fact]

namespace SalesSample.UnitTests.Application;

public sealed class CreateSaleHandlerTests
{
    // ── INSTÂNCIA do Faker (é ela que tem .Company, .Address, ...) ──
    private static readonly Faker Faker = new("pt_BR");

    static CreateSaleHandlerTests()
    {
        // Bogus determinístico (semente fixa → dados reprodutíveis)
        Randomizer.Seed = new Random(42);
    }

    // ┌──────────────────────────────────────────────────────────────┐
    // │ ARRANGE (compartilhado)                                      │
    // │ 1 Mock<T> por dependência. O handler tem 6 dependências.     │
    // │ Mock<T> = o "dublê"; .Object = o objeto que o SUT recebe.    │
    // └──────────────────────────────────────────────────────────────┘
    private readonly Mock<ISaleService> _saleServiceMock = new();
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Mock<IBranchRepository> _branchRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<ISaleRepository> _saleRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly CreateSaleHandler _sut; // SUT = System Under Test

    public CreateSaleHandlerTests()
    {
        // Constrói o SUT injetando os dublês (.Object
        _sut = new CreateSaleHandler(
            _saleServiceMock.Object,
            _customerRepositoryMock.Object,
            _branchRepositoryMock.Object,
            _productRepositoryMock.Object,
            _saleRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    // ┌──────────────────────────────────────────────────────────────┐
    // │ OBJECT MOTHER / TEST DATA BUILDER                            │
    // │ Um lugar só cria dados VÁLIDOS. Muda o formato num lugar só. │
    // └──────────────────────────────────────────────────────────────┘
    private static Customer CriarCustomer() => new(Faker.Company.CompanyName());
    private static Branch CriarBranch() => new(Faker.Address.City());

    private static Product CriarProduct(decimal preco = 100m) =>
        new(Faker.Commerce.ProductName(), new Money(preco, "BRL"));

    private static CreateSaleCommand CriarCommand(
        Guid customerId, Guid branchId, Guid productId, int quantity = 4) =>
        new(customerId, branchId, null, new[] { new CreateSaleItem(productId, quantity) });

    // =================================================================
    // TESTE 1 — caminho feliz
    // =================================================================
    [Fact(DisplayName = "Handle deve criar a venda e persistir quando tudo existe")]
    public async Task Handle_DeveCriarVendaEPersistir()
    {
        // ─────────────────────────── ARRANGE ───────────────────────────
        var customer = CriarCustomer();
        var branch = CriarBranch();
        var product = CriarProduct();

        var sale = new Sale(customer, branch, "SALE-TEST-001");
        sale.AddItem(product, 4);

        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _branchRepositoryMock
            .Setup(r => r.GetByIdAsync(branch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch);

        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { product });

        _saleServiceMock
            .Setup(s => s.CreateSale(customer, branch, It.IsAny<string?>(),
                It.IsAny<IEnumerable<(Product, int)>>()))
            .Returns(sale);

        // ───────────────────────────── ACT ─────────────────────────────
        var result = await _sut.Handle(
            CriarCommand(customer.Id, branch.Id, product.Id, 4),
            CancellationToken.None);

        // ──────────────────────────── ASSERT ───────────────────────────
        result.SaleId.ShouldBe(sale.Id);
        result.TotalAmount.ShouldBe(360m); // 100 * 4 * 0.9

        _saleRepositoryMock.Verify(
            r => r.AddAsync(sale, It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =================================================================
    // TESTE 2 — cliente inexistente
    // =================================================================
    [Fact(DisplayName = "Handle deve lançar quando o cliente não existe")]
    public async Task Handle_DeveLancar_QuandoClienteNaoExiste()
    {
        // ─────────────────────────── ARRANGE ───────────────────────────
        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // ───────────────────────────── ACT ─────────────────────────────
        Func<Task> act = () => _sut.Handle(
            CriarCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        // ──────────────────────────── ASSERT ───────────────────────────
        await Should.ThrowAsync<NotFoundException>(act);
    }

    // =================================================================
    // TESTE 3 — filial inexistente
    // =================================================================
    [Fact(DisplayName = "Handle deve lançar quando a filial não existe")]
    public async Task Handle_DeveLancar_QuandoFilialNaoExiste()
    {
        // ─────────────────────────── ARRANGE ───────────────────────────
        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarCustomer());

        _branchRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        // ───────────────────────────── ACT ─────────────────────────────
        Func<Task> act = () => _sut.Handle(
            CriarCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        // ──────────────────────────── ASSERT ───────────────────────────
        await Should.ThrowAsync<NotFoundException>(act);
    }

    // =================================================================
    // TESTE 4 — produto inexistente
    // =================================================================
    [Fact(DisplayName = "Handle deve lançar quando o produto não existe")]
    public async Task Handle_DeveLancar_QuandoProdutoNaoExiste()
    {
        // ─────────────────────────── ARRANGE ───────────────────────────
        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarCustomer());

        _branchRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarBranch());

        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Product>());

        // ───────────────────────────── ACT ─────────────────────────────
        Func<Task> act = () => _sut.Handle(
            CriarCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        // ──────────────────────────── ASSERT ───────────────────────────
        await Should.ThrowAsync<NotFoundException>(act);
    }
}