namespace ERP.Fiscal.Abstractions;

/// <summary>Resultado neutro de consulta auxiliar de CNPJ.</summary>
public class NfeConsultaCnpjResult
{
    public bool Sucesso { get; init; }
    public string? RazaoSocial { get; init; }
    public string? NomeFantasia { get; init; }
    public string? Logradouro { get; init; }
    public string? Numero { get; init; }
    public string? Complemento { get; init; }
    public string? Bairro { get; init; }
    public string? Municipio { get; init; }
    public string? Uf { get; init; }
    public string? Cep { get; init; }
    public string? Telefone { get; init; }
    public string? Email { get; init; }
    public string? Mensagem { get; init; }
    public int HttpStatusCode { get; init; }
    public string? RawResponse { get; init; }
}

/// <summary>Resultado neutro de consulta auxiliar de CEP.</summary>
public class NfeConsultaCepResult
{
    public bool Sucesso { get; init; }
    public string? Logradouro { get; init; }
    public string? Bairro { get; init; }
    public string? Municipio { get; init; }
    public string? Uf { get; init; }
    public string? Cep { get; init; }
    public string? CodigoIbge { get; init; }
    public string? Mensagem { get; init; }
    public int HttpStatusCode { get; init; }
    public string? RawResponse { get; init; }
}
