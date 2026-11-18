using Microsoft.EntityFrameworkCore;
using TransactionTypeService.Api.Dtos;
using TransactionTypeService.Api.Error;
using TransactionTypeService.Api.Models;
using TransactionTypeService.Api.Repositories;
using TransactionTypeService.Api.Validation;
using Xunit;
using Svc = TransactionTypeService.Api.Services.TransactionTypeService;

namespace TransactionTypeService.Tests;

public class TransactionTypeServiceTests
{
    private static readonly (string Code, string Desc)[] Seed =
    {
        ("01", "PURCHASE"), ("02", "PAYMENT"), ("03", "CREDIT"), ("04", "AUTHORIZATION"),
        ("05", "REFUND"), ("06", "REVERAL"), ("07", "ADJUSTMENT"), ("08", "FEE"), ("09", "INTEREST")
    };

    private static (Svc svc, TransactionTypeContext ctx) Create()
    {
        var options = new DbContextOptionsBuilder<TransactionTypeContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new TransactionTypeContext(options);
        ctx.TransactionTypes.AddRange(Seed.Select(s => new TransactionType { TypeCode = s.Code, Description = s.Desc }));
        ctx.SaveChanges();
        return (new Svc(ctx), ctx);
    }

    [Fact]
    public async Task FirstPage_ReturnsSevenRows_WithNextAndNoPrevious()
    {
        var (svc, _) = Create();
        var page = await svc.GetTransactionTypesAsync(null, null, null, null, null);
        Assert.Equal(7, page.Items.Count());
        Assert.Equal("01", page.Items.First().TypeCode);
        Assert.True(page.HasNext);
        Assert.False(page.HasPrevious);
        Assert.Equal("07", page.NextCursor);
    }

    [Fact]
    public async Task ForwardFromCursor_ReturnsLastPage_WithPreviousAndNoNext()
    {
        var (svc, _) = Create();
        var page = await svc.GetTransactionTypesAsync(null, null, "07", "forward", null);
        Assert.Equal(new[] { "08", "09" }, page.Items.Select(i => i.TypeCode));
        Assert.False(page.HasNext);
        Assert.True(page.HasPrevious);
    }

    [Fact]
    public async Task BackwardFromCursor_ReturnsRowsInAscendingOrder()
    {
        var (svc, _) = Create();
        var page = await svc.GetTransactionTypesAsync(null, null, "08", "backward", null);
        Assert.Equal(new[] { "01", "02", "03", "04", "05", "06", "07" }, page.Items.Select(i => i.TypeCode));
        Assert.False(page.HasPrevious);
    }

    [Fact]
    public async Task DescriptionFilter_IsCaseInsensitiveContains()
    {
        var (svc, _) = Create();
        var page = await svc.GetTransactionTypesAsync(null, "pay", null, null, null);
        Assert.Equal("02", Assert.Single(page.Items).TypeCode);
    }

    [Fact]
    public async Task FilterWithNoMatch_ReturnsEmptyPage()
    {
        var (svc, _) = Create();
        var page = await svc.GetTransactionTypesAsync("99", null, null, null, null);
        Assert.Empty(page.Items);
        Assert.False(page.HasNext);
    }

    [Theory]
    [InlineData("A1", null, null)]
    [InlineData(null, "1", null)]
    [InlineData(null, null, 8)]
    public async Task InvalidListParameters_Throw(string? typeCode, string? cursor, int? size)
    {
        var (svc, _) = Create();
        await Assert.ThrowsAsync<ValidationException>(() => svc.GetTransactionTypesAsync(typeCode, null, cursor, null, size));
    }

    [Fact]
    public async Task GetMissing_ThrowsNotFound()
    {
        var (svc, _) = Create();
        await Assert.ThrowsAsync<TransactionTypeNotFoundException>(() => svc.GetTransactionTypeAsync("55"));
    }

    [Fact]
    public async Task Create_ThenGet_ReturnsTrimmedDescription()
    {
        var (svc, _) = Create();
        await svc.CreateTransactionTypeAsync(new CreateTransactionTypeRequest { TypeCode = "10", Description = "  CASHBACK " });
        var dto = await svc.GetTransactionTypeAsync("10");
        Assert.Equal("CASHBACK", dto.Description);
    }

    [Fact]
    public async Task Update_SameDescriptionIgnoringCase_ReportsNoChange()
    {
        var (svc, _) = Create();
        var (dto, changed) = await svc.UpdateTransactionTypeAsync("01", new UpdateTransactionTypeRequest { Description = "purchase" });
        Assert.False(changed);
        Assert.Null(dto);
    }

    [Fact]
    public async Task Update_ChangesDescription()
    {
        var (svc, ctx) = Create();
        var (_, changed) = await svc.UpdateTransactionTypeAsync("06", new UpdateTransactionTypeRequest { Description = "REVERSAL" });
        Assert.True(changed);
        Assert.Equal("REVERSAL", ctx.TransactionTypes.AsNoTracking().Single(t => t.TypeCode == "06").Description);
    }

    [Fact]
    public async Task Update_Missing_WithoutCreateIfMissing_ThrowsConcurrentDelete()
    {
        var (svc, _) = Create();
        await Assert.ThrowsAsync<ConcurrentDeleteException>(() =>
            svc.UpdateTransactionTypeAsync("55", new UpdateTransactionTypeRequest { Description = "X" }));
    }

    [Fact]
    public async Task Update_Missing_WithCreateIfMissing_Inserts()
    {
        var (svc, _) = Create();
        var (dto, changed) = await svc.UpdateTransactionTypeAsync("55", new UpdateTransactionTypeRequest { Description = "NEW", CreateIfMissing = true });
        Assert.True(changed);
        Assert.Equal("55", dto!.TypeCode);
    }

    [Fact]
    public async Task Delete_RemovesRow_AndSecondDeleteThrowsNotFound()
    {
        var (svc, _) = Create();
        await svc.DeleteTransactionTypeAsync("09");
        await Assert.ThrowsAsync<TransactionTypeNotFoundException>(() => svc.DeleteTransactionTypeAsync("09"));
    }
}
