using ERP.Fiscal.PlugNotas.Contracts;
using ERP.Fiscal.PlugNotas.Payload;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Payload;

public class PlugNotasNfePayloadReadinessTests
{
    [Fact]
    public void Avaliar_deve_rejeitar_documento_nulo()
    {
        var (_, pendencias) = PlugNotasNfePayloadReadiness.Avaliar(null);

        pendencias.ShouldNotBeEmpty();
    }

    [Fact]
    public void CombinacaoInvalidaPlugNotas_deve_detectar_presencial_zero_com_finalidade_normal()
    {
        PlugNotasNfeNaturezaCamposHelper.CombinacaoInvalidaPlugNotas("0", 1).ShouldBeTrue();
        PlugNotasNfeNaturezaCamposHelper.CombinacaoInvalidaPlugNotas("0", 2).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(7)]
    public void FinalidadeInvalida_deve_rejeitar_valores_fora_da_faixa(int? finalidade)
    {
        PlugNotasNfeNaturezaCamposHelper.FinalidadeInvalida(finalidade).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void FinalidadeInvalida_deve_aceitar_faixa_oficial(int finalidade)
    {
        PlugNotasNfeNaturezaCamposHelper.FinalidadeInvalida(finalidade).ShouldBeFalse();
    }
}
