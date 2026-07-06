using ERP.Fiscal.PlugNotas.Payload;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Payload;

public class PlugNotasNfeTributosPayloadHelperTests
{
    [Fact]
    public void BuildSimplesNacional_deve_preencher_baseCalculo_e_aliquota_zerados_no_icms()
    {
        var entrada = new PlugNotasNfeTributosPayloadHelper.SimplesNacionalEntrada(
            IcmsCst: "40",
            IcmsModalidadeDeterminacao: 0,
            PisCst: "99",
            CofinsCst: "07");

        var tributos = PlugNotasNfeTributosPayloadHelper.BuildSimplesNacional(entrada);

        tributos.Icms.ShouldNotBeNull();
        tributos.Icms!.Cst.ShouldBe("40");
        tributos.Icms.BaseCalculo.ShouldNotBeNull();
        tributos.Icms.BaseCalculo!.Valor.ShouldBe(0);
        tributos.Icms.Aliquota.ShouldBe(0);
        tributos.Icms.Valor.ShouldBe(0);

        tributos.Pis!.BaseCalculo!.Valor.ShouldBe(0);
        tributos.Pis.Aliquota.ShouldBe(0);
        tributos.Cofins!.BaseCalculo!.Valor.ShouldBe(0);
        tributos.Cofins.Aliquota.ShouldBe(0);
    }

    [Fact]
    public void BuildRegimeNormal_deve_calcular_bases_e_valores()
    {
        var entrada = new PlugNotasNfeTributosPayloadHelper.RegimeNormalEntrada(
            IcmsCst: "00",
            IcmsModalidadeDeterminacao: 3,
            IcmsAliquotaPercentual: 18,
            IcmsBaseCalculoPercentual: 100,
            PisCst: "01",
            PisAliquotaPercentual: 1.65m,
            PisBaseCalculoPercentual: 100,
            CofinsCst: "01",
            CofinsAliquotaPercentual: 7.6m,
            CofinsBaseCalculoPercentual: 100,
            ValorTotalItem: 100m,
            QuantidadeItem: 2m);

        var tributos = PlugNotasNfeTributosPayloadHelper.BuildRegimeNormal(entrada);

        tributos.Icms!.BaseCalculo!.Valor.ShouldBe(100m);
        tributos.Icms.Aliquota.ShouldBe(18);
        tributos.Icms.Valor.ShouldBe(18m);
        tributos.Pis!.BaseCalculo!.Quantidade.ShouldBe(2m);
        tributos.Pis.Valor.ShouldBe(1.65m);
        tributos.Cofins!.Valor.ShouldBe(7.6m);
    }
}
