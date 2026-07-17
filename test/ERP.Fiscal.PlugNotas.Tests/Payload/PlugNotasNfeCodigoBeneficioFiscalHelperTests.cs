using ERP.Fiscal.PlugNotas.Payload;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Payload;

public class PlugNotasNfeCodigoBeneficioFiscalHelperTests
{
    [Theory]
    [InlineData("20")]
    [InlineData("30")]
    [InlineData("40")]
    [InlineData("41")]
    [InlineData("50")]
    [InlineData("51")]
    [InlineData("70")]
    [InlineData("90")]
    public void deve_aceitar_codigo_apenas_nos_csts_compativeis(string cst)
    {
        PlugNotasNfeCodigoBeneficioFiscalHelper.CstAceitaCodigoBeneficioFiscal(cst).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00")]
    [InlineData("10")]
    public void deve_rejeitar_codigo_nos_demais_csts(string? cst)
    {
        PlugNotasNfeCodigoBeneficioFiscalHelper.CstAceitaCodigoBeneficioFiscal(cst).ShouldBeFalse();
    }

    [Fact]
    public void deve_normalizar_codigo_quando_cst_aceita()
    {
        PlugNotasNfeCodigoBeneficioFiscalHelper.ObterCodigoParaPayload(" 51 ", " PR830001 ")
            .ShouldBe("PR830001");
    }

    [Fact]
    public void deve_omitir_codigo_vazio_ou_cst_incompativel()
    {
        PlugNotasNfeCodigoBeneficioFiscalHelper.ObterCodigoParaPayload("51", " ").ShouldBeNull();
        PlugNotasNfeCodigoBeneficioFiscalHelper.ObterCodigoParaPayload("00", "PR830001").ShouldBeNull();
    }
}
