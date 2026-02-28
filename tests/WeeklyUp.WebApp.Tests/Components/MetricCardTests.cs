using WeeklyUp.WebApp.Components.Dashboard;

namespace WeeklyUp.WebApp.Tests.Components;

public sealed class MetricCardTests : TestContext
{
    public MetricCardTests()
    {
        Services.AddMudServices(o => o.SnackbarConfiguration.ShowTransitionDuration = 0);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void MetricCard_RendersLabelAndValue()
    {
        // Arrange & Act
        var cut = RenderComponent<MetricCard>(parameters => parameters
            .Add(p => p.Label, "Receita")
            .Add(p => p.Value, "R$ 1.500,00"));

        // Assert
        cut.Markup.Should().Contain("Receita");
        cut.Markup.Should().Contain("1.500,00");
    }

    [Fact]
    public void MetricCard_WithSubValue_RendersSubValue()
    {
        // Arrange & Act
        var cut = RenderComponent<MetricCard>(parameters => parameters
            .Add(p => p.Label, "Vendas")
            .Add(p => p.Value, "42")
            .Add(p => p.SubValue, "Ticket médio: R$ 35,00"));

        // Assert
        cut.Markup.Should().Contain("Ticket médio: R$ 35,00");
    }

    [Fact]
    public void MetricCard_WithoutSubValue_DoesNotRenderSubValue()
    {
        // Arrange & Act
        var cut = RenderComponent<MetricCard>(parameters => parameters
            .Add(p => p.Label, "Visitas")
            .Add(p => p.Value, "1.234"));

        // Assert
        cut.Instance.SubValue.Should().BeNullOrEmpty();
    }
}
