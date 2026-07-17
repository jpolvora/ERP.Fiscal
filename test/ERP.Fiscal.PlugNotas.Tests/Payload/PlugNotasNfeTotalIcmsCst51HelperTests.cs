using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Fiscal.PlugNotas.Contracts;
using ERP.Fiscal.PlugNotas.Payload;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Payload;

public class PlugNotasNfeTotalIcmsCst51HelperTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void aplicar_total_cst_51_deve_preencher_valor_icms_igual_soma_itens()
    {
        var doc = CriarDocumentoComItemIcms(cst: "51", valorIcms: 357m);

        PlugNotasNfeTotalIcmsCst51Helper.AplicarTotalValorIcmsQuandoCst51(doc);

        doc.Total.ShouldNotBeNull();
        doc.Total!.ValorIcms.ShouldBe(357m);

        var json = Serializar(doc);
        json.ShouldContain("\"total\"");
        using var parsed = JsonDocument.Parse(json);
        parsed.RootElement[0].GetProperty("total").GetProperty("valorIcms").GetDecimal().ShouldBe(357m);
    }

    [Fact]
    public void aplicar_total_exemplo_cst_51_valor_357()
    {
        var doc = new PlugNotasNfeDocumentPayload
        {
            Itens =
            [
                new PlugNotasNfeItemPayload
                {
                    Tributos = new PlugNotasNfeTributosItemPayload
                    {
                        Icms = new PlugNotasNfeTributoIcmsPayload
                        {
                            Cst = "51",
                            BaseCalculo = new PlugNotasNfeBaseCalculoIcmsPayload { Valor = 2100m },
                            Aliquota = 17m,
                            Valor = 357m
                        }
                    }
                }
            ]
        };

        PlugNotasNfeTotalIcmsCst51Helper.AplicarTotalValorIcmsQuandoCst51(doc);

        doc.Total!.ValorIcms.ShouldBe(357m);
    }

    [Fact]
    public void aplicar_total_sem_cst_51_nao_deve_emitir_no_json()
    {
        var doc = CriarDocumentoComItemIcms(cst: "00", valorIcms: 100m);

        PlugNotasNfeTotalIcmsCst51Helper.AplicarTotalValorIcmsQuandoCst51(doc);

        doc.Total.ShouldBeNull();
        Serializar(doc).ShouldNotContain("\"total\"");
    }

    [Fact]
    public void aplicar_total_lote_misto_soma_apenas_itens_cst_51()
    {
        var doc = new PlugNotasNfeDocumentPayload
        {
            Itens =
            [
                CriarItemIcms("51", 357m),
                CriarItemIcms("00", 200m)
            ]
        };

        PlugNotasNfeTotalIcmsCst51Helper.AplicarTotalValorIcmsQuandoCst51(doc);

        doc.Total!.ValorIcms.ShouldBe(357m);
    }

    [Fact]
    public void aplicar_total_cst_51_com_espacos_deve_reconhecer()
    {
        var doc = CriarDocumentoComItemIcms(cst: " 51 ", valorIcms: 50m);

        PlugNotasNfeTotalIcmsCst51Helper.AplicarTotalValorIcmsQuandoCst51(doc);

        doc.Total!.ValorIcms.ShouldBe(50m);
    }

    [Fact]
    public void aplicar_total_dois_itens_cst_51_deve_somar()
    {
        var doc = new PlugNotasNfeDocumentPayload
        {
            Itens =
            [
                CriarItemIcms("51", 100m),
                CriarItemIcms("51", 257m)
            ]
        };

        PlugNotasNfeTotalIcmsCst51Helper.AplicarTotalValorIcmsQuandoCst51(doc);

        doc.Total!.ValorIcms.ShouldBe(357m);
    }

    [Fact]
    public void aplicar_total_no_json_deve_preservar_payload_invalido_e_atualizar_payload_valido()
    {
        const string invalido = "{not-json";
        PlugNotasNfeTotalIcmsCst51Helper.AplicarTotalValorIcmsQuandoCst51NoPayloadJson(invalido)
            .ShouldBe(invalido);

        var payload = Serializar(CriarDocumentoComItemIcms("51", 357m));
        var atualizado = PlugNotasNfeTotalIcmsCst51Helper.AplicarTotalValorIcmsQuandoCst51NoPayloadJson(payload);

        using var parsed = JsonDocument.Parse(atualizado);
        parsed.RootElement[0].GetProperty("total").GetProperty("valorIcms").GetDecimal().ShouldBe(357m);
    }

    private static PlugNotasNfeDocumentPayload CriarDocumentoComItemIcms(string cst, decimal valorIcms) =>
        new()
        {
            Itens = [CriarItemIcms(cst, valorIcms)]
        };

    private static PlugNotasNfeItemPayload CriarItemIcms(string cst, decimal valorIcms) =>
        new()
        {
            Tributos = new PlugNotasNfeTributosItemPayload
            {
                Icms = new PlugNotasNfeTributoIcmsPayload
                {
                    Cst = cst,
                    Valor = valorIcms
                }
            }
        };

    private static string Serializar(PlugNotasNfeDocumentPayload doc) =>
        JsonSerializer.Serialize(new List<PlugNotasNfeDocumentPayload> { doc }, JsonOptions);
}
