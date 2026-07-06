using ERP.Fiscal.PlugNotas.Parsers;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Parsers;

public class PlugNotasNfeEmissaoRespostaParserTests
{
    [Fact]
    public void TryParse_deve_extrair_id_documento_e_protocolo_do_formato_documents()
    {
        const string raw = """
            { "documents": [{ "id": "aabbccddeeff00112233ddee", "protocolo": "135240000123456" }], "protocol": "5f1a2b3c-guid-lote" }
            """;

        var result = PlugNotasNfeEmissaoRespostaParser.TryParse(raw);

        result.ShouldNotBeNull();
        result!.IdDocumentoProvedor.ShouldBe("aabbccddeeff00112233ddee");
        result.ProtocoloAutorizacaoSefaz.ShouldBe("135240000123456");
    }

    [Fact]
    public void TryParse_deve_retornar_nulo_para_json_invalido()
    {
        PlugNotasNfeEmissaoRespostaParser.TryParse("não é json").ShouldBeNull();
    }

    [Fact]
    public void TryParse_deve_retornar_campos_nulos_quando_ausentes()
    {
        var result = PlugNotasNfeEmissaoRespostaParser.TryParse("{ \"message\": \"ok\" }");

        result.ShouldNotBeNull();
        result!.IdDocumentoProvedor.ShouldBeNull();
        result.ProtocoloAutorizacaoSefaz.ShouldBeNull();
    }

    [Theory]
    [InlineData("aabbccddeeff00112233ddee", true)]
    [InlineData("id-curto", false)]
    [InlineData(null, false)]
    public void LooksLikeIdDocumentoPlugNotas_deve_validar_hex_24_caracteres(string? value, bool esperado)
    {
        PlugNotasNfeEmissaoRespostaParser.LooksLikeIdDocumentoPlugNotas(value).ShouldBe(esperado);
    }

    [Fact]
    public void IndicaDocumentoJaExistenteNoProvedor_deve_detectar_mensagem_de_duplicidade()
    {
        var indica = PlugNotasNfeEmissaoRespostaParser.IndicaDocumentoJaExistenteNoProvedor(
            sucesso: false,
            rawBody: "{ \"message\": \"já existe uma nfe com esse idIntegracao\" }",
            errorMessage: null);

        indica.ShouldBeTrue();
    }

    [Fact]
    public void IndicaDocumentoJaExistenteNoProvedor_deve_retornar_falso_quando_sucesso()
    {
        PlugNotasNfeEmissaoRespostaParser.IndicaDocumentoJaExistenteNoProvedor(true, "qualquer coisa", null).ShouldBeFalse();
    }

    [Fact]
    public void TryExtrairCodigoRejeicao_deve_ler_cStat_aninhado()
    {
        const string raw = """{ "data": { "erros": [{ "cStat": 204 }] } }""";

        PlugNotasNfeEmissaoRespostaParser.TryExtrairCodigoRejeicao(raw).ShouldBe("204");
    }
}
