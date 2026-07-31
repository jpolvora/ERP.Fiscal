using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Fiscal.PlugNotas.Contracts;
using ERP.Fiscal.PlugNotas.Payload;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Payload;

public class PlugNotasNfeIcmsCamposPorCstHelperTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Theory]
    [InlineData("40")]
    [InlineData("41")]
    [InlineData("50")]
    [InlineData("103")]
    [InlineData("300")]
    [InlineData("400")]
    public void cst_sem_destaque_deve_omitir_valor_e_aliquota_no_json(string cst)
    {
        var tributos = PlugNotasNfeTributosPayloadHelper.BuildSimplesNacional(
            new PlugNotasNfeTributosPayloadHelper.SimplesNacionalEntrada(
                IcmsCst: cst,
                IcmsModalidadeDeterminacao: 0,
                PisCst: "99",
                CofinsCst: "07"));

        var json = JsonSerializer.Serialize(tributos.Icms, JsonOptions);
        using var node = JsonDocument.Parse(json);
        var root = node.RootElement;

        root.GetProperty("cst").GetString().ShouldBe(cst);
        root.TryGetProperty("valor", out _).ShouldBeFalse();
        root.TryGetProperty("aliquota", out _).ShouldBeFalse();
        tributos.Icms!.Valor.ShouldBeNull();
        tributos.Icms.Aliquota.ShouldBeNull();
    }

    [Fact]
    public void cst_00_deve_manter_valor_e_aliquota()
    {
        var tributos = PlugNotasNfeTributosPayloadHelper.BuildRegimeNormal(
            new PlugNotasNfeTributosPayloadHelper.RegimeNormalEntrada(
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
                QuantidadeItem: 1m));

        tributos.Icms!.Valor.ShouldBe(18m);
        tributos.Icms.Aliquota.ShouldBe(18);
    }

    [Fact]
    public void cst_51_deve_manter_valor_para_totalizador()
    {
        var tributos = PlugNotasNfeTributosPayloadHelper.BuildRegimeNormal(
            new PlugNotasNfeTributosPayloadHelper.RegimeNormalEntrada(
                IcmsCst: "51",
                IcmsModalidadeDeterminacao: 3,
                IcmsAliquotaPercentual: 17,
                IcmsBaseCalculoPercentual: 100,
                PisCst: "01",
                PisAliquotaPercentual: 0,
                PisBaseCalculoPercentual: 100,
                CofinsCst: "01",
                CofinsAliquotaPercentual: 0,
                CofinsBaseCalculoPercentual: 100,
                ValorTotalItem: 100m,
                QuantidadeItem: 1m));

        tributos.Icms!.Valor.ShouldBe(17m);
    }

    [Fact]
    public void cst_60_deve_omitir_base_calculo_raiz()
    {
        var icms = new PlugNotasNfeTributoIcmsPayload
        {
            Origem = "0",
            Cst = "60",
            BaseCalculo = new PlugNotasNfeBaseCalculoIcmsPayload { ModalidadeDeterminacao = 0, Valor = 0 },
            Aliquota = 0,
            Valor = 0
        };

        PlugNotasNfeIcmsCamposPorCstHelper.AplicarCamposPermitidos(icms);

        icms.BaseCalculo.ShouldBeNull();
        icms.Valor.ShouldBeNull();
        icms.Aliquota.ShouldBeNull();
    }
}
