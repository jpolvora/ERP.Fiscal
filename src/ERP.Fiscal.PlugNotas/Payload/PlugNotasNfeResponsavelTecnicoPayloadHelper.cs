using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Contracts;

namespace ERP.Fiscal.PlugNotas.Payload;

/// <summary>Mapeia opções neutras da lib para o bloco <c>responsavelTecnico</c> do payload PlugNotas.</summary>
public static class PlugNotasNfeResponsavelTecnicoPayloadHelper
{
    public static PlugNotasNfeResponsavelTecnicoPayload? MapFromOptions(PlugNotasNfeResponsavelTecnicoOptions? options)
    {
        if (options == null)
        {
            return null;
        }

        var doc = FiscalDigitsHelper.DigitsOnly(options.CpfCnpj);
        if (doc.Length != 11 && doc.Length != 14)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(options.Nome))
        {
            return null;
        }

        var ddd = FiscalDigitsHelper.DigitsOnly(options.TelefoneDdd);
        var tel = FiscalDigitsHelper.DigitsOnly(options.TelefoneNumero);
        PlugNotasNfeTelefonePayload? fone = null;
        if (ddd.Length >= 2 && tel.Length >= 8)
        {
            fone = new PlugNotasNfeTelefonePayload
            {
                Ddd = ddd[..2],
                Numero = tel
            };
        }

        return new PlugNotasNfeResponsavelTecnicoPayload
        {
            CpfCnpj = doc,
            Nome = options.Nome.Trim(),
            Email = NullIfWhiteSpace(options.Email),
            Telefone = fone
        };
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
