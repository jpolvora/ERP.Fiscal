using System;

namespace ERP.Fiscal.PlugNotas.Configuration;

/// <summary>
/// Erro de configuração da lib (ex.: ApiKey de produção ausente). O ERP consumidor deve capturar
/// esta exceção e traduzi-la para sua própria <c>BusinessException</c> localizada, se desejar.
/// </summary>
public class PlugNotasConfigurationException : Exception
{
    public PlugNotasConfigurationException(string message) : base(message)
    {
    }
}
