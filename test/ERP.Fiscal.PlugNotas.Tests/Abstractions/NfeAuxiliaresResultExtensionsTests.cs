using ERP.Fiscal.Abstractions;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Abstractions;

public class NfeAuxiliaresResultExtensionsTests
{
    [Fact]
    public void TemDadosCadastrais_cep_com_municipio_deve_ser_true()
    {
        new NfeConsultaCepResult { Municipio = "Maringá" }.TemDadosCadastrais().ShouldBeTrue();
    }

    [Fact]
    public void TemDadosCadastrais_cep_sem_dados_deve_ser_false()
    {
        new NfeConsultaCepResult().TemDadosCadastrais().ShouldBeFalse();
    }

    [Fact]
    public void TemDadosCadastrais_cnpj_com_razao_social_deve_ser_true()
    {
        new NfeConsultaCnpjResult { RazaoSocial = "Empresa" }.TemDadosCadastrais().ShouldBeTrue();
    }

    [Fact]
    public void TemDadosCadastrais_cnpj_sem_razao_social_deve_ser_false()
    {
        new NfeConsultaCnpjResult().TemDadosCadastrais().ShouldBeFalse();
    }
}
