using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ERP.Fiscal.PlugNotas.Contracts;

/// <summary>Contratos JSON para NF-e na API PlugNotas (POST <c>/nfe</c>, array na raiz).</summary>
public class PlugNotasNfeDocumentPayload
{
    public const string IntermediadorSemMarketplace = "0";

    public string? IdIntegracao { get; set; }
    public string? InformacoesComplementares { get; set; }
    public string? Presencial { get; set; }
    public string Intermediador { get; set; } = IntermediadorSemMarketplace;
    public bool ConsumidorFinal { get; set; }

    [JsonPropertyName("natureza")]
    public string? Natureza { get; set; }

    [JsonPropertyName("finalidade")]
    public int? Finalidade { get; set; }

    [JsonPropertyName("tipo")]
    public int? Tipo { get; set; }

    public string? DataEmissao { get; set; }
    public string? Serie { get; set; }
    public int? Numero { get; set; }
    public PlugNotasNfeEmitentePayload? Emitente { get; set; }
    public PlugNotasNfeDestinatarioPayload? Destinatario { get; set; }
    public List<PlugNotasNfeItemPayload>? Itens { get; set; }
    public List<PlugNotasNfePagamentoPayload>? Pagamentos { get; set; }
    public PlugNotasNfeResponsavelTecnicoPayload? ResponsavelTecnico { get; set; }
    public PlugNotasNfeTransportePayload? Transporte { get; set; }
}

public class PlugNotasNfeEmitentePayload
{
    public string? CpfCnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
}

public class PlugNotasNfeDestinatarioPayload
{
    public string? CpfCnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
    public int? IndicadorInscricaoEstadual { get; set; }
    public string? RazaoSocial { get; set; }
    public string? Email { get; set; }
    public PlugNotasNfeEnderecoDestPayload? Endereco { get; set; }
}

public class PlugNotasNfeEnderecoDestPayload
{
    public string? TipoLogradouro { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cep { get; set; }
    public string? CodigoCidade { get; set; }
    public string? DescricaoCidade { get; set; }
    public string? Estado { get; set; }
}

public class PlugNotasNfeItemPayload
{
    public string? Codigo { get; set; }
    public string? Descricao { get; set; }
    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public string? Cfop { get; set; }

    private string? _codigoBeneficioFiscal;

    public string? CodigoBeneficioFiscal
    {
        get => _codigoBeneficioFiscal;
        set => _codigoBeneficioFiscal = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public PlugNotasNfeValorUnitarioComercialPayload? ValorUnitario { get; set; }
    public decimal Valor { get; set; }
    public PlugNotasNfeComercialTributavelStringPayload? Unidade { get; set; }
    public PlugNotasNfeComercialTributavelDecimalPayload? Quantidade { get; set; }

    [JsonPropertyName("tributos")]
    public PlugNotasNfeTributosItemPayload? Tributos { get; set; }
}

public class PlugNotasNfeComercialTributavelStringPayload
{
    public string? Comercial { get; set; }
    public string? Tributavel { get; set; }
}

public class PlugNotasNfeComercialTributavelDecimalPayload
{
    public decimal Comercial { get; set; }
    public decimal Tributavel { get; set; }
}

public class PlugNotasNfeValorUnitarioComercialPayload
{
    public decimal Comercial { get; set; }
    public decimal Tributavel { get; set; }
}

public class PlugNotasNfePagamentoPayload
{
    public bool AVista { get; set; }
    public string? Meio { get; set; }
    public decimal Valor { get; set; }
}

public class PlugNotasNfeResponsavelTecnicoPayload
{
    public string? CpfCnpj { get; set; }
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public PlugNotasNfeTelefonePayload? Telefone { get; set; }
}

public class PlugNotasNfeTelefonePayload
{
    public string? Ddd { get; set; }
    public string? Numero { get; set; }
}

public class PlugNotasNfeTransportePayload
{
    public int ModalidadeFrete { get; set; }
    public PlugNotasNfeTransportadoraPayload? Transportadora { get; set; }
    public PlugNotasNfeVeiculoPayload? Veiculo { get; set; }
}

public class PlugNotasNfeTransportadoraPayload
{
    public string? CpfCnpj { get; set; }
    public string? RazaoSocial { get; set; }
}

public class PlugNotasNfeVeiculoPayload
{
    public string? Placa { get; set; }
    public string? Uf { get; set; }
}
