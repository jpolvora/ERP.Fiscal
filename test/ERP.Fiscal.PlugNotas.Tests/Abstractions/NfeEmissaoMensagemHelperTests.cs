using ERP.Fiscal.Abstractions;
using Shouldly;
using Xunit;

namespace ERP.Fiscal.PlugNotas.Tests.Abstractions;

public class NfeEmissaoMensagemHelperTests
{
    [Fact]
    public void MontarMensagemErro_deve_montar_mensagem_transiente_com_detalhe_e_acao()
    {
        var result = new NfeEmissaoResult
        {
            IsTransientFailure = true,
            Mensagem = "timeout"
        };

        var msg = NfeEmissaoMensagemHelper.MontarMensagemErro(
            "Falha temporária.",
            "Falha permanente.",
            "Tente novamente.",
            result);

        msg.ShouldContain("Falha temporária.");
        msg.ShouldContain("timeout");
        msg.ShouldContain("Tente novamente.");
    }

    [Fact]
    public void MontarMensagemErro_deve_montar_mensagem_permanente_quando_nao_transiente()
    {
        var result = new NfeEmissaoResult
        {
            IsTransientFailure = false,
            Mensagem = "Rejeição SEFAZ"
        };

        var msg = NfeEmissaoMensagemHelper.MontarMensagemErro(
            "Falha temporária.",
            "Falha permanente.",
            "Tente novamente.",
            result);

        msg.ShouldStartWith("Falha permanente.");
        msg.ShouldContain("Rejeição SEFAZ");
        msg.ShouldNotContain("Tente novamente.");
    }

    [Fact]
    public void MontarMensagemErro_deve_respeitar_max_length()
    {
        var msg = NfeEmissaoMensagemHelper.MontarMensagemErro(
            "Erro.",
            "Erro permanente com texto longo.",
            acaoTransiente: "",
            result: null,
            detalheProvedorOuExcecao: new string('x', 100),
            maxLength: 20);

        msg.Length.ShouldBe(20);
    }

    [Fact]
    public void EhFalhaTransiente_deve_detectar_emissao_e_processamento()
    {
        NfeEmissaoMensagemHelper.EhFalhaTransiente(new NfeEmissaoResult { IsTransientFailure = true })
            .ShouldBeTrue();
        NfeEmissaoMensagemHelper.EhFalhaTransiente(new NfeProcessamentoResult { IsTransientFailure = true })
            .ShouldBeTrue();
        NfeEmissaoMensagemHelper.EhFalhaTransiente((NfeEmissaoResult?)null).ShouldBeFalse();
    }
}
