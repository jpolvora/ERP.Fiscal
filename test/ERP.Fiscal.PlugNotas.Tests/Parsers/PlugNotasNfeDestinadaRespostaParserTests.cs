using ERP.Fiscal.PlugNotas.Parsers;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Parsers;

public class PlugNotasNfeDestinadaRespostaParserTests
{
    [Fact]
    public void Parse_deve_mapear_lista_em_data()
    {
        const string json = """
            {
              "data": [
                {
                  "chave": "35260112345678000199550010000000011000000012",
                  "cnpjEmitente": "12345678000199",
                  "cnpjDestinatario": "98765432000188",
                  "dataEmissao": "2026-01-15T10:00:00Z",
                  "nomeResumo": "Fornecedor Teste",
                  "resumoItensNcm": "4407"
                }
              ]
            }
            """;

        var result = PlugNotasNfeDestinadaRespostaParser.Parse(200, json);

        result.Sucesso.ShouldBeTrue();
        result.Itens.Count.ShouldBe(1);
        result.Itens[0].ChaveAcesso.ShouldStartWith("3526");
        result.Itens[0].CnpjEmitente.ShouldBe("12345678000199");
    }
}
