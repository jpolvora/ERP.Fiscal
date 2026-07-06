using ERP.Fiscal.PlugNotas.Parsers;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Parsers;

public class PlugNotasHttpErrorClassifierTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public void IsTransient_deve_retornar_true_para_status_retentaveis(int statusCode)
    {
        PlugNotasHttpErrorClassifier.IsTransient(statusCode).ShouldBeTrue();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(422)]
    [InlineData(500)]
    public void IsTransient_deve_retornar_false_para_status_nao_retentaveis(int statusCode)
    {
        PlugNotasHttpErrorClassifier.IsTransient(statusCode).ShouldBeFalse();
    }
}
