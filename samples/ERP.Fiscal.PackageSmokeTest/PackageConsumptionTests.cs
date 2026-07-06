using ERP.Fiscal.Abstractions;
using ERP.Fiscal.PlugNotas;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;

namespace ERP.Fiscal.PackageSmokeTest;

public class PackageConsumptionTests
{
    [Fact]
    public void Abstractions_pacote_expoem_contratos_neutros()
    {
        typeof(INfeEmissaoProvider).IsInterface.ShouldBeTrue();
        typeof(NfeAmbiente).IsEnum.ShouldBeTrue();
        FiscalDigitsHelper.DigitsOnly("12.345.678/0001-90").ShouldBe("12345678000190");
    }

    [Fact]
    public void PlugNotas_pacote_expoem_modulo_abp()
    {
        typeof(PlugNotasFiscalModule).IsClass.ShouldBeTrue();
        typeof(AbpModule).IsAssignableFrom(typeof(PlugNotasFiscalModule)).ShouldBeTrue();
    }
}
