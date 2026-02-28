using WeeklyUp.WebApp.Components.Reports;
using WeeklyUp.WebApp.Models;

namespace WeeklyUp.WebApp.Tests.Components;

public sealed class ReportStatusChipTests : TestContext
{
    public ReportStatusChipTests()
    {
        Services.AddMudServices(o => o.SnackbarConfiguration.ShowTransitionDuration = 0);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData(ReportStatus.Completed, "Concluído")]
    [InlineData(ReportStatus.Pending, "Pendente")]
    [InlineData(ReportStatus.Failed, "Falhou")]
    public void ReportStatusChip_ShowsCorrectLabelForKnownStatus(string status, string expectedLabel)
    {
        // Arrange & Act
        var cut = RenderComponent<ReportStatusChip>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        cut.Markup.Should().Contain(expectedLabel);
    }

    [Fact]
    public void ReportStatusChip_UnknownStatus_ShowsStatusAsLabel()
    {
        // Arrange & Act
        var cut = RenderComponent<ReportStatusChip>(parameters => parameters
            .Add(p => p.Status, "Generating"));

        // Assert
        cut.Markup.Should().Contain("Generating");
    }
}
