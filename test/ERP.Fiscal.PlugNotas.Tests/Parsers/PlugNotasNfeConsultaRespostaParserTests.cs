using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Parsers;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Parsers;

public class PlugNotasNfeConsultaRespostaParserTests
{
    [Fact]
    public void TryParse_deve_classificar_como_autorizada_quando_cStat_100()
    {
        const string raw = """{ "cStat": 100, "protocolo": "135240000123456" }""";

        var result = PlugNotasNfeConsultaRespostaParser.TryParse(raw);

        result.ShouldNotBeNull();
        result!.SituacaoResumida.ShouldBe(NfeSituacao.Autorizada);
    }

    [Theory]
    [InlineData(110)]
    [InlineData(205)]
    [InlineData(301)]
    public void TryParse_deve_classificar_como_rejeitada_para_cstat_de_rejeicao(int cStat)
    {
        var raw = $$"""{ "cStat": {{cStat}} }""";

        var result = PlugNotasNfeConsultaRespostaParser.TryParse(raw);

        result!.SituacaoResumida.ShouldBe(NfeSituacao.Rejeitada);
    }

    [Fact]
    public void TryParse_deve_classificar_como_cancelada_para_cstat_135()
    {
        var result = PlugNotasNfeConsultaRespostaParser.TryParse("""{ "cStat": 135 }""");

        result!.SituacaoResumida.ShouldBe(NfeSituacao.Cancelada);
    }

    [Fact]
    public void TryParse_deve_ler_primeiro_elemento_quando_raiz_e_array()
    {
        const string raw = """[{ "status": "CONCLUIDO", "numero": "123", "serie": "1" }]""";

        var result = PlugNotasNfeConsultaRespostaParser.TryParse(raw);

        result.ShouldNotBeNull();
        result!.NumeroNota.ShouldBe("123");
        result.SituacaoResumida.ShouldBe(NfeSituacao.Autorizada);
    }

    [Fact]
    public void TryParse_deve_retornar_desconhecido_quando_nao_ha_indicativo_de_situacao()
    {
        var result = PlugNotasNfeConsultaRespostaParser.TryParse("""{ "algumCampo": "valor" }""");

        result!.SituacaoResumida.ShouldBe(NfeSituacao.Desconhecido);
    }

    [Fact]
    public void TryParse_deve_retornar_nulo_para_corpo_vazio()
    {
        PlugNotasNfeConsultaRespostaParser.TryParse(null).ShouldBeNull();
        PlugNotasNfeConsultaRespostaParser.TryParse("").ShouldBeNull();
    }
}
