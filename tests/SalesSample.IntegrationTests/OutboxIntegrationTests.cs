using Microsoft.EntityFrameworkCore;
using SalesSample.Domain.Entities;
using SalesSample.Domain.ValueObjects;
using SalesSample.Infrastructure.Persistence;
using Shouldly;

namespace SalesSample.IntegrationTests;

/// <summary>
/// TESTES DE INTEGRAÇÃO — 2º andar da pirâmide.
///
/// Pergunta que respondem: "funciona com o banco de verdade?"
/// Cobrem o que o unitário NÃO pega: SQL, FK, índice único, jsonb e TRANSAÇÃO.
///
/// O alvo aqui é o Transactional Outbox:
///   - a venda e o evento entram na MESMA transação (atomicidade);
///   - se a transação falha, NENHUM evento vaza (rollback).
/// </summary>
public sealed class OutboxIntegrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public OutboxIntegrationTests(PostgresFixture fixture) => _fixture = fixture;

    // CancellationToken do xunit (elimina o warning xUnit1051).
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// TRUNCATE no Arrange: o container é COMPARTILHADO entre os testes,
    /// então limpamos pra cada teste começar do zero (isolamento).
    /// CASCADE por causa das FKs (SaleItems -> Sales -> Products, etc).
    /// </summary>
    private static async Task LimparAsync(SalesDbContext db) =>
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE \"SaleItems\", \"Sales\", \"OutboxMessages\", \"InboxMessages\", " +
            "\"Products\", \"Customers\", \"Branches\" CASCADE", Ct);

    /// <summary>
    /// OBJECT MOTHER: cria e persiste os dados de base (dados VÁLIDOS num lugar só).
    /// Retorna a tupla porque os testes precisam usar os objetos (com Id já gerado).
    /// </summary>
    private static async Task<(Customer, Branch, Product)> SemearAsync(SalesDbContext db)
    {
        var customer = new Customer("Fazenda Santa Fé");
        var branch = new Branch("Filial São Paulo");
        var product = new Product("Fertilizante NPK", new Money(100m, "BRL"));

        db.Customers.Add(customer);
        db.Branches.Add(branch);
        db.Products.Add(product);
        await db.SaveChangesAsync(Ct);

        return (customer, branch, product);
    }

    // =================================================================
    // 1) SUCESSO — a atomicidade do Outbox
    // =================================================================
    [Fact(DisplayName = "SaveChanges deve gravar a venda e o evento do Outbox na mesma transação")]
    public async Task SaveChangesAsync_DeveGravarVendaEOutboxNaMesmaTransacao()
    {
        // ── ARRANGE ──────────────────────────────────────────────
        await using var db = _fixture.CreateContext();
        await LimparAsync(db);

        var (customer, branch, product) = await SemearAsync(db);

        var sale = new Sale(customer, branch, "SALE-INT-001");
        sale.AddItem(product, 4);   // 4 itens -> 10% de desconto

        // ── ACT ──────────────────────────────────────────────────
        // Aqui está a MÁGICA: o agregado Sale tem o SaleCreatedEvent na memória.
        // O SaveChangesAsync do SalesDbContext varre os DomainEvents e grava
        // o OutboxMessage NA MESMA TRANSAÇÃO da venda.
        db.Sales.Add(sale);
        await db.SaveChangesAsync(Ct);

        // ── ASSERT ───────────────────────────────────────────────
        // (a) O Outbox foi preenchido AUTOMATICAMENTE (ninguém chamou Insert nele).
        var outbox = await db.OutboxMessages.SingleAsync(Ct);
        outbox.Type.ShouldBe("SaleCreatedEvent");
        outbox.ProcessedOn.ShouldBeNull();               // ainda NÃO foi pro Kafka
        outbox.Payload.ShouldContain("SALE-INT-001");    // evento serializado em jsonb

        // (b) A venda existe no banco, com os itens (Include = JOIN real).
        var savedSale = await db.Sales.Include(s => s.Items).SingleAsync(Ct);
        savedSale.SaleNumber.ShouldBe("SALE-INT-001");
        savedSale.Items.ShouldHaveSingleItem();
    }

    // =================================================================
    // 2) O OUTBOX NÃO DUPLICA
    // =================================================================
    [Fact(DisplayName = "Salvar duas vezes sem mudança de domínio não deve duplicar o Outbox")]
    public async Task SaveChangesAsync_NaoDeveDuplicarOutbox_QuandoNadaMuda()
    {
        // ── ARRANGE ──────────────────────────────────────────────
        await using var db = _fixture.CreateContext();
        await LimparAsync(db);

        var (customer, branch, product) = await SemearAsync(db);

        var sale = new Sale(customer, branch, "SALE-INT-002");
        sale.AddItem(product, 4);
        db.Sales.Add(sale);
        await db.SaveChangesAsync(Ct);   // 1º save -> gera 1 evento

        // ── ACT ──────────────────────────────────────────────────
        await db.SaveChangesAsync(Ct);   // 2º save, sem mudança de domínio

        // ── ASSERT ───────────────────────────────────────────────
        // Continua 1 evento -> o ClearDomainEvents() limpou a lista depois do 1º save.
        // Se não limpasse, o 2º save geraria um evento DUPLICADO (bug clássico).
        (await db.OutboxMessages.CountAsync(Ct)).ShouldBe(1);
    }

    // =================================================================
    // 3) FALHA — o rollback (o teste que MAIS prova o Outbox)
    // =================================================================
    [Fact(DisplayName = "Se a transação falha (SaleNumber duplicado), o evento do Outbox NÃO deve vazar")]
    public async Task SaveChangesAsync_DeveRollback_QuandoViolarUniqueDeSaleNumber()
    {
        // ── ARRANGE ──────────────────────────────────────────────
        await using var db = _fixture.CreateContext();
        await LimparAsync(db);

        var (customer, branch, product) = await SemearAsync(db);

        var sale1 = new Sale(customer, branch, "SALE-DUP");
        sale1.AddItem(product, 2);
        db.Sales.Add(sale1);
        await db.SaveChangesAsync(Ct);

        var outboxAntes = await db.OutboxMessages.CountAsync(Ct);   // = 1

        // ── ACT ──────────────────────────────────────────────────
        // 2ª venda com o MESMO SaleNumber -> viola o índice único do banco.
        var sale2 = new Sale(customer, branch, "SALE-DUP");
        sale2.AddItem(product, 2);
        db.Sales.Add(sale2);

        Func<Task> act = () => db.SaveChangesAsync(Ct);

        // ── ASSERT ───────────────────────────────────────────────
        await Should.ThrowAsync<DbUpdateException>(act);

        // A transação INTEIRA voltou:
        //   - a 2ª venda NÃO entrou;
        //   - o evento da 2ª venda NÃO vazou pro Outbox.
        // É a prova de que o "dual-write" foi eliminado de verdade.
        (await db.OutboxMessages.CountAsync(Ct)).ShouldBe(outboxAntes);
        (await db.Sales.CountAsync(Ct)).ShouldBe(1);
    }
}