using System.Text.Json;
using System.Text.Json.Nodes;
using ERP.Fiscal.Abstractions;
using Microsoft.Extensions.Logging;

namespace ERP.Fiscal.PlugNotas.Payload;

/// <summary>
/// Ajusta o JSON do POST <c>/nfe</c> PlugNotas antes do envio HTTP — injeta <c>config.producao</c>
/// e o indicador de intermediador padrão. Opera apenas sobre o texto JSON (sem depender de tipos de domínio),
/// para que cada ERP possa aplicá-lo ao payload já montado por seu próprio <c>NfePayloadBuilder</c>.
/// </summary>
/// <remarks>
/// Contrato PlugNotas: corpo é um <b>array JSON na raiz</b> (um elemento = um documento).
/// SEFAZ distingue ambiente pelo XML; no payload isso é refletido em <c>nfe[].config.producao</c>
/// (<c>false</c> homologação/sandbox, <c>true</c> produção).
/// </remarks>
public static class PlugNotasNfePayloadAmbienteHelper
{
    private const string IntermediadorSemMarketplace = "0";

    /// <summary>Injeta <c>config.producao</c> em cada documento do array.</summary>
    /// <param name="payloadJson">JSON do POST <c>/nfe</c> (array na raiz, um documento por elemento).</param>
    /// <param name="ambiente">Ambiente efetivo; <see cref="NfeAmbiente.Producao"/> define <c>config.producao</c> como <c>true</c>.</param>
    /// <param name="logger">Opcional. Recomendado em produção: registra raiz inesperada (não-array) sem mascarar erros de parse.</param>
    /// <exception cref="JsonException">JSON inválido onde o parse exige documento (não engolido).</exception>
    public static string AplicarProducaoNoPayloadJson(string payloadJson, NfeAmbiente ambiente, ILogger? logger = null)
    {
        var producao = ambiente == NfeAmbiente.Producao;
        var node = JsonNode.Parse(payloadJson);
        if (node is not JsonArray arr)
        {
            logger?.LogWarning(
                "POST /nfe PlugNotas: JSON raiz não é array; config.producao não aplicado. " +
                "O ERP deve serializar um array na raiz (um elemento por documento).");
            return payloadJson;
        }

        foreach (var item in arr)
        {
            if (item is not JsonObject doc)
                continue;

            if (doc["intermediador"] == null)
                doc["intermediador"] = IntermediadorSemMarketplace;

            var cfg = doc["config"] as JsonObject ?? new JsonObject();
            if (doc["config"] == null)
                doc["config"] = cfg;

            cfg["producao"] = producao;
        }

        return node.ToJsonString();
    }
}
