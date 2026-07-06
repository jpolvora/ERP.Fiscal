namespace ERP.Fiscal.Abstractions;

/// <summary>
/// Ambiente fiscal neutro de provedor. Cada ERP mapeia seu próprio enum de domínio
/// (ex.: <c>AmbienteFiscal</c>) para este valor através de um método de extensão local —
/// evita referência cruzada entre a lib e o domínio do produto.
/// </summary>
public enum NfeAmbiente
{
    /// <summary>Ambiente mock do provedor (ex.: sandbox PlugNotas) — sem validade fiscal.</summary>
    Sandbox = 0,

    /// <summary>Homologação SEFAZ via API oficial do provedor.</summary>
    Homologacao = 1,

    /// <summary>Produção SEFAZ via API oficial do provedor.</summary>
    Producao = 2
}
