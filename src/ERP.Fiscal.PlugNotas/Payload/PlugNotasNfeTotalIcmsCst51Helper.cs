using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Fiscal.PlugNotas.Contracts;

namespace ERP.Fiscal.PlugNotas.Payload;

/// <summary>
/// Workaround Tecnospeed/PlugNotas: quando há item com CST ICMS 51,
/// envia <c>total.valorIcms</c> = soma dos <c>tributos.icms.valor</c> desses itens.
/// Chamar no builder do ERP consumidor após montar <see cref="PlugNotasNfeDocumentPayload"/>, antes de serializar.
/// </summary>
public static class PlugNotasNfeTotalIcmsCst51Helper
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void AplicarTotalValorIcmsQuandoCst51(PlugNotasNfeDocumentPayload doc)
    {
        if (doc.Itens == null || doc.Itens.Count == 0)
        {
            doc.Total = null;
            return;
        }

        decimal soma = 0m;
        var temCst51 = false;

        foreach (var item in doc.Itens)
        {
            var cst = item.Tributos?.Icms?.Cst?.Trim();
            if (cst != "51")
                continue;

            temCst51 = true;
            soma += item.Tributos!.Icms!.Valor ?? 0m;
        }

        doc.Total = temCst51
            ? new PlugNotasNfeTotalPayload { ValorIcms = soma }
            : null;
    }

    /// <summary>Sincroniza <c>total.valorIcms</c> em um payload JSON do POST <c>/nfe</c>.</summary>
    public static string AplicarTotalValorIcmsQuandoCst51NoPayloadJson(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return payloadJson;

        List<PlugNotasNfeDocumentPayload>? docs;
        try
        {
            docs = JsonSerializer.Deserialize<List<PlugNotasNfeDocumentPayload>>(payloadJson, PayloadJsonOptions);
        }
        catch (JsonException)
        {
            return payloadJson;
        }

        if (docs == null || docs.Count == 0)
            return payloadJson;

        foreach (var doc in docs)
            AplicarTotalValorIcmsQuandoCst51(doc);

        return JsonSerializer.Serialize(docs, PayloadJsonOptions);
    }
}
