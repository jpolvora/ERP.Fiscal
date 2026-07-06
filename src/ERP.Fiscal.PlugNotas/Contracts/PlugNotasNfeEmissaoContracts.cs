namespace ERP.Fiscal.PlugNotas.Contracts;

/// <summary>Bloco <c>itens[].tributos</c> do POST <c>/nfe</c> PlugNotas.</summary>
public class PlugNotasNfeTributosItemPayload
{
    public PlugNotasNfeTributoIcmsPayload? Icms { get; set; }
    public PlugNotasNfeTributoPisPayload? Pis { get; set; }
    public PlugNotasNfeTributoCofinsPayload? Cofins { get; set; }
}

public class PlugNotasNfeTributoIcmsPayload
{
    public string? Origem { get; set; }
    public string? Cst { get; set; }
    public PlugNotasNfeBaseCalculoIcmsPayload? BaseCalculo { get; set; }
    public decimal? Aliquota { get; set; }
    public decimal? Valor { get; set; }
}

public class PlugNotasNfeBaseCalculoIcmsPayload
{
    public int ModalidadeDeterminacao { get; set; }
    public decimal Valor { get; set; }
}

public class PlugNotasNfeTributoPisPayload
{
    public string? Cst { get; set; }
    public PlugNotasNfeBaseCalculoPisPayload? BaseCalculo { get; set; }
    public decimal? Aliquota { get; set; }
    public decimal? Valor { get; set; }
}

public class PlugNotasNfeBaseCalculoPisPayload
{
    public decimal Valor { get; set; }
    public decimal Quantidade { get; set; }
}

public class PlugNotasNfeTributoCofinsPayload
{
    public string? Cst { get; set; }
    public PlugNotasNfeBaseCalculoCofinsPayload? BaseCalculo { get; set; }
    public decimal? Aliquota { get; set; }
    public decimal? Valor { get; set; }
}

public class PlugNotasNfeBaseCalculoCofinsPayload
{
    public decimal Valor { get; set; }
}
