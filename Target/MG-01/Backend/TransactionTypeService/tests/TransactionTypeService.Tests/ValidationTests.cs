using TransactionTypeService.Api.Domain;
using TransactionTypeService.Api.Dtos;
using TransactionTypeService.Api.Errors;
using TransactionTypeService.Api.Validation;

namespace TransactionTypeService.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("5", "05")]
    [InlineData("05", "05")]
    [InlineData(" 7 ", "07")]
    [InlineData("99", "99")]
    public void Code_is_normalised_to_two_digits(string raw, string expected)
    {
        Assert.True(TransactionTypeCode.TryNormalize(raw, out var code, out _));
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData(null, "Type code is required.")]
    [InlineData("  ", "Type code is required.")]
    [InlineData("AB", "Type code must be numeric.")]
    [InlineData("1A", "Type code must be numeric.")]
    [InlineData("123", "Type code must be numeric.")]
    [InlineData("00", "Type code must not be zero.")]
    [InlineData("0", "Type code must not be zero.")]
    public void Invalid_code_is_rejected(string? raw, string message)
    {
        Assert.False(TransactionTypeCode.TryNormalize(raw, out _, out var error));
        Assert.Equal(message, error);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("0", null)]
    [InlineData("00", null)]
    [InlineData("5", "05")]
    public void Filter_blank_or_zero_means_no_filter(string? raw, string? expected)
    {
        Assert.True(TransactionTypeCode.TryNormalizeFilter(raw, out var code));
        Assert.Equal(expected, code);
    }

    [Fact]
    public void Non_numeric_filter_is_rejected()
    {
        var ex = Assert.Throws<ServiceException>(() => TransactionTypeValidators.ValidateList(new ListQuery { TypeCode = "A1" }));
        Assert.Equal(ErrorCodes.TypeFilterInvalid, ex.Code);
        Assert.Equal("Type code must be a 2-digit number.", ex.Message);
    }

    [Theory]
    [InlineData(null, "Description is required.")]
    [InlineData("   ", "Description is required.")]
    [InlineData("Purchase-Online", "Description can contain only letters, numbers and spaces.")]
    [InlineData("Purchase Online 2", null)]
    public void Description_rules(string? value, string? message) =>
        Assert.Equal(message, TransactionTypeValidators.DescriptionError(value));

    [Fact]
    public void Description_longer_than_50_is_rejected() =>
        Assert.Equal("Description can be at most 50 characters.", TransactionTypeValidators.DescriptionError(new string('A', 51)));

    [Fact]
    public void Create_reports_both_fields()
    {
        var ex = Assert.Throws<ServiceException>(() => TransactionTypeValidators.ValidateCreate(new CreateTransactionTypeRequest("", "")));
        Assert.Equal(ErrorCodes.ValidationFailed, ex.Code);
        Assert.Contains("typeCode", ex.Errors!.Keys);
        Assert.Contains("description", ex.Errors!.Keys);
    }

    [Fact]
    public void Paging_keys_cannot_both_be_set()
    {
        var ex = Assert.Throws<ServiceException>(() => TransactionTypeValidators.ValidateList(new ListQuery { After = "03", Before = "05" }));
        Assert.Equal("Use either after or before, not both.", ex.Errors!["after"][0]);
    }

    [Fact]
    public void Default_page_size_is_seven() =>
        Assert.Equal(7, TransactionTypeValidators.ValidateList(new ListQuery()).PageSize);
}
