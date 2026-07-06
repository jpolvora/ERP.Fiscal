using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ERP.Fiscal.PlugNotas.Tests;

/// <summary>Handler de teste que retorna respostas pré-programadas em sequência e registra as requisições recebidas.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = new();

    /// <summary>Corpo da requisição lido no momento do envio (a requisição original é descartada pelo chamador logo após).</summary>
    public List<string> RequestBodies { get; } = new();

    public FakeHttpMessageHandler Enqueue(HttpStatusCode statusCode, string body)
    {
        _responses.Enqueue(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body)
        });
        return this;
    }

    public FakeHttpMessageHandler Enqueue(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
        _responses.Enqueue(factory);
        return this;
    }

    public FakeHttpMessageHandler EnqueueNetworkFailure()
    {
        _responses.Enqueue(_ => throw new HttpRequestException("Falha simulada de rede."));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        if (_responses.Count == 0)
            throw new InvalidOperationException("Nenhuma resposta programada para esta requisição.");

        return _responses.Dequeue()(request);
    }

    public static HttpClient CreateClient(FakeHttpMessageHandler handler) => new(handler);
}
