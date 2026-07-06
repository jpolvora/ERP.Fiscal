using ERP.Fiscal.Abstractions;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Abstractions;

public class FiscalDigitsHelperTests
{
    [Theory]
    [InlineData("87111-001", "87111001")]
    [InlineData(" 4115-200 ", "4115200")]
    [InlineData(null, "")]
    [InlineData("abc", "")]
    public void DigitsOnly_deve_retornar_apenas_digitos(string? input, string esperado)
    {
        FiscalDigitsHelper.DigitsOnly(input).ShouldBe(esperado);
    }

    [Theory]
    [InlineData("87111-001", "87111001")]
    [InlineData("abc", null)]
    [InlineData(null, null)]
    public void DigitsOnlyOrNull_deve_retornar_null_quando_sem_digitos(string? input, string? esperado)
    {
        FiscalDigitsHelper.DigitsOnlyOrNull(input).ShouldBe(esperado);
    }
}
