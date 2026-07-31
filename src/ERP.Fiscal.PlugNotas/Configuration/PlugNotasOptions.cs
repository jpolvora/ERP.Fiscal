namespace ERP.Fiscal.PlugNotas.Configuration;

/// <summary>Configuração da integração PlugNotas (appsettings seção <see cref="SectionName"/>).</summary>
public class PlugNotasOptions
{
    public const string SectionName = "PlugNotas";

    /// <summary>
    /// Token <c>x-api-key</c> opcional para ambiente <see cref="ERP.Fiscal.Abstractions.NfeAmbiente.Sandbox"/>
    /// (<c>api.sandbox.plugnotas.com.br</c>). Se vazio, usa <see cref="PlugNotasAmbienteConstants.PublicSandboxApiKey"/>.
    /// </summary>
    public string SandboxApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Token <c>x-api-key</c> para Homologação e Produção (<c>api.plugnotas.com.br</c>), obtido no portal PlugNotas.
    /// </summary>
    public string ProductionApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Quando <c>true</c>, o ERP consumidor pode forçar runtime sempre em Sandbox independentemente do
    /// ambiente cadastrado no emissor (decisão fica em <c>INfeAmbientePolicy</c> de cada ERP).
    /// </summary>
    public bool OnlySandbox { get; set; } = true;

    /// <summary>
    /// Modelo de faturamento PlugNotas (<c>nfe.tipoContrato</c> e <c>nfe.config.tipoContrato</c> no POST /empresa):
    /// <c>0</c> Bilhetagem; <c>1</c> Ilimitado. Valores fora disso são normalizados para <c>1</c>.
    /// </summary>
    public int TipoContrato { get; set; } = 1;

    /// <summary>
    /// Opcional. Se preenchido (CPF/CNPJ + nome), sobrescreve o responsável técnico padrão do emissor no POST <c>/nfe</c>.
    /// </summary>
    public PlugNotasNfeResponsavelTecnicoOptions? ResponsavelTecnico { get; set; }

    /// <summary>Política de retry para POST <c>/nfe</c> em falhas transitórias.</summary>
    public PlugNotasRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// TTL em minutos do cache da lista <c>GET /nfse/cidades</c>. Default 360 (6h). Mínimo efetivo: 1.
    /// </summary>
    public int MunicipiosCacheMinutes { get; set; } = 360;

    public static int NormalizeTipoContrato(int value) => value == 0 ? 0 : 1;

    public int GetEffectiveMaxAttempts() => Retry.MaxAttempts < 1 ? 1 : Retry.MaxAttempts;

    public int GetEffectiveBaseDelayMs() => Retry.BaseDelayMs < 100 ? 100 : Retry.BaseDelayMs;

    public int GetEffectiveMunicipiosCacheMinutes() =>
        MunicipiosCacheMinutes < 1 ? 1 : MunicipiosCacheMinutes;
}

/// <summary>Configuração de tentativas para emissão NF-e na PlugNotas.</summary>
public class PlugNotasRetryOptions
{
    /// <summary>Número máximo de tentativas (inclui a primeira). Mínimo efetivo: 1.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Delay base em ms para backoff exponencial (1ª retry = base, 2ª = 2×, 3ª = 4×).</summary>
    public int BaseDelayMs { get; set; } = 1000;
}

/// <summary>Dados do responsável técnico no JSON da NF-e.</summary>
public class PlugNotasNfeResponsavelTecnicoOptions
{
    public string? CpfCnpj { get; set; }
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? TelefoneDdd { get; set; }
    public string? TelefoneNumero { get; set; }
}
