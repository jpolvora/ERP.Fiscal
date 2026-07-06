using System.Text.Json;
using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Payload;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Configuration;

public class PlugNotasNfePayloadAmbienteHelperTests
{
    [Fact]
    public void AplicarProducaoNoPayloadJson_deve_injetar_config_producao_true_em_producao()
    {
        const string payload = """[{ "idIntegracao": "abc" }]""";

        var result = PlugNotasNfePayloadAmbienteHelper.AplicarProducaoNoPayloadJson(payload, NfeAmbiente.Producao);

        using var doc = JsonDocument.Parse(result);
        var doc0 = doc.RootElement[0];
        doc0.GetProperty("config").GetProperty("producao").GetBoolean().ShouldBeTrue();
        doc0.GetProperty("intermediador").GetString().ShouldBe("0");
    }

    [Theory]
    [InlineData(NfeAmbiente.Homologacao)]
    [InlineData(NfeAmbiente.Sandbox)]
    public void AplicarProducaoNoPayloadJson_deve_injetar_config_producao_false_fora_de_producao(NfeAmbiente ambiente)
    {
        const string payload = """[{ "idIntegracao": "abc" }]""";

        var result = PlugNotasNfePayloadAmbienteHelper.AplicarProducaoNoPayloadJson(payload, ambiente);

        using var doc = JsonDocument.Parse(result);
        doc.RootElement[0].GetProperty("config").GetProperty("producao").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void AplicarProducaoNoPayloadJson_deve_retornar_payload_original_quando_raiz_nao_e_array()
    {
        const string payload = """{ "nfe": [] }""";

        var result = PlugNotasNfePayloadAmbienteHelper.AplicarProducaoNoPayloadJson(payload, NfeAmbiente.Producao);

        result.ShouldBe(payload);
    }
}
