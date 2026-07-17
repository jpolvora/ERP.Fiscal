using System.Collections.Generic;
using ERP.Fiscal.PlugNotas.Contracts;
using ERP.Fiscal.PlugNotas.Payload;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Payload;

public class PlugNotasNfsePayloadReadinessTests
{
    [Fact]
    public void Avaliar_deve_rejeitar_documento_nulo()
    {
        var (pode, pendencias) = PlugNotasNfsePayloadReadiness.Avaliar(null);

        pode.ShouldBeFalse();
        pendencias.ShouldNotBeEmpty();
    }

    [Fact]
    public void TryParseDocumentoFromPostArray_deve_ler_primeiro_item_do_array()
    {
        var json = """
            [{
              "idIntegracao": "doc-1",
              "competencia": "2026-07-01",
              "prestador": { "cpfCnpj": "12345678000199", "inscricaoMunicipal": "12345" },
              "servico": [{
                "codigo": "1.01",
                "codigoCidadeIncidencia": "3550308",
                "discriminacao": "Serviço de teste",
                "iss": { "exigibilidade": 1, "aliquota": 5 },
                "valor": { "servico": 100 }
              }]
            }]
            """;

        var doc = PlugNotasNfsePayloadReadiness.TryParseDocumentoFromPostArray(json);

        doc.ShouldNotBeNull();
        doc!.IdIntegracao.ShouldBe("doc-1");
        doc.Prestador!.CpfCnpj.ShouldBe("12345678000199");
        doc.Servico!.Count.ShouldBe(1);
    }

    [Fact]
    public void TryParseDocumentoFromPostArray_deve_retornar_null_para_json_invalido()
    {
        PlugNotasNfsePayloadReadiness.TryParseDocumentoFromPostArray("{not-json").ShouldBeNull();
        PlugNotasNfsePayloadReadiness.TryParseDocumentoFromPostArray(null).ShouldBeNull();
        PlugNotasNfsePayloadReadiness.TryParseDocumentoFromPostArray("").ShouldBeNull();
    }

    [Fact]
    public void Avaliar_deve_listar_pendencias_estruturais()
    {
        var doc = new PlugNotasNfseDocumentPayload
        {
            Servico = new List<PlugNotasNfseServicoPayload>
            {
                new()
            }
        };

        var (pode, pendencias) = PlugNotasNfsePayloadReadiness.Avaliar(doc);

        pode.ShouldBeFalse();
        pendencias.ShouldContain(x => x.Contains("idIntegracao"));
        pendencias.ShouldContain(x => x.Contains("Competência"));
        pendencias.ShouldContain(x => x.Contains("prestador"));
        pendencias.ShouldContain(x => x.Contains("código do serviço"));
    }

    [Fact]
    public void Avaliar_deve_aceitar_payload_minimo_valido()
    {
        var doc = CriarDocumentoMinimoValido();

        var (pode, pendencias) = PlugNotasNfsePayloadReadiness.Avaliar(doc);

        pode.ShouldBeTrue();
        pendencias.ShouldBeEmpty();
    }

    private static PlugNotasNfseDocumentPayload CriarDocumentoMinimoValido() => new()
    {
        IdIntegracao = "doc-1",
        Competencia = "2026-07-01",
        Prestador = new PlugNotasNfsePrestadorPayload
        {
            CpfCnpj = "12345678000199",
            InscricaoMunicipal = "12345"
        },
        Servico =
        [
            new PlugNotasNfseServicoPayload
            {
                Codigo = "1.01",
                CodigoCidadeIncidencia = "3550308",
                Discriminacao = "Consultoria",
                Iss = new PlugNotasNfseIssPayload
                {
                    Exigibilidade = 1,
                    Aliquota = 5m
                },
                Valor = new PlugNotasNfseValorPayload
                {
                    Servico = 150m,
                    BaseCalculo = 150m
                }
            }
        ]
    };
}
