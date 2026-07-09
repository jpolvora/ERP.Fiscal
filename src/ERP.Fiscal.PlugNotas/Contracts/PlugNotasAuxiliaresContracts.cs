using System.Collections.Generic;

namespace ERP.Fiscal.PlugNotas.Contracts;

internal sealed class PlugNotasConsultaCnpjDados
{
    public string? Status { get; set; }
    public string? CpfCnpj { get; set; }
    public string? Message { get; set; }
    public PlugNotasConsultaAuxiliarEnderecoDados? Endereco { get; set; }
    public string? Nome { get; set; }
    public string? RazaoSocial { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
}

internal sealed class PlugNotasConsultaAuxiliarEnderecoDados
{
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Municipio { get; set; }
    public string? Uf { get; set; }
}

internal sealed class PlugNotasConsultaCepDados
{
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Municipio { get; set; }
    public string? Uf { get; set; }
    public string? Ibge { get; set; }
    public string? CodigoIbge { get; set; }
    public string? CodigoMunicipio { get; set; }
}

/// <summary>Item de <c>GET /nfse/cidades</c> (campos usados no autocomplete).</summary>
internal sealed class PlugNotasNfseCidadeDados
{
    public long? Id { get; set; }
    public string? Nome { get; set; }
    public string? Uf { get; set; }
}
