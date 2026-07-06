using ERP.Fiscal.PlugNotas.Configuration;
using ERP.Fiscal.PlugNotas.Payload;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Payload;

public class PlugNotasNfeResponsavelTecnicoPayloadHelperTests
{
    [Fact]
    public void MapFromOptions_deve_mapear_campos_validos()
    {
        var options = new PlugNotasNfeResponsavelTecnicoOptions
        {
            CpfCnpj = "12.345.678/0001-90",
            Nome = " Responsavel Fiscal ",
            Email = " contato@empresa.com ",
            TelefoneDdd = "(44)",
            TelefoneNumero = "99876-5432"
        };

        var payload = PlugNotasNfeResponsavelTecnicoPayloadHelper.MapFromOptions(options);

        payload.ShouldNotBeNull();
        payload!.CpfCnpj.ShouldBe("12345678000190");
        payload.Nome.ShouldBe("Responsavel Fiscal");
        payload.Email.ShouldBe("contato@empresa.com");
        payload.Telefone.ShouldNotBeNull();
        payload.Telefone!.Ddd.ShouldBe("44");
        payload.Telefone.Numero.ShouldBe("998765432");
    }

    [Fact]
    public void MapFromOptions_deve_retornar_null_quando_documento_invalido()
    {
        var options = new PlugNotasNfeResponsavelTecnicoOptions
        {
            CpfCnpj = "123",
            Nome = "Responsavel"
        };

        PlugNotasNfeResponsavelTecnicoPayloadHelper.MapFromOptions(options).ShouldBeNull();
    }

    [Fact]
    public void MapFromOptions_deve_retornar_null_quando_nome_ausente()
    {
        var options = new PlugNotasNfeResponsavelTecnicoOptions
        {
            CpfCnpj = "12345678901",
            Nome = " "
        };

        PlugNotasNfeResponsavelTecnicoPayloadHelper.MapFromOptions(options).ShouldBeNull();
    }
}
