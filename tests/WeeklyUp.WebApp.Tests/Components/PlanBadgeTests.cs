using WeeklyUp.WebApp.Components.Shared;
using WeeklyUp.WebApp.Models;

namespace WeeklyUp.WebApp.Tests.Components;

public sealed class PlanBadgeTests : TestContext
{
    public PlanBadgeTests()
    {
        Services.AddMudServices(o => o.SnackbarConfiguration.ShowTransitionDuration = 0);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData(PlanNames.Pro, "Pro")]
    [InlineData(PlanNames.Business, "Business")]
    public void PlanBadge_PaidPlan_ShowsPlanName(string plan, string expectedText)
    {
        // Arrange & Act
        var cut = RenderComponent<PlanBadge>(parameters => parameters
            .Add(p => p.Plan, plan));

        // Assert
        cut.Markup.Should().Contain(expectedText);
    }

    [Fact]
    public void PlanBadge_FreePlan_ShowsGratis()
    {
        // Free plan maps to "Grátis" label (see PlanBadge.razor OnParametersSet)
        var cut = RenderComponent<PlanBadge>(parameters => parameters
            .Add(p => p.Plan, PlanNames.Free));

        cut.Markup.Should().Contain("Grátis");
    }

    [Fact]
    public void PlanBadge_UnknownPlan_ShowsGratis()
    {
        // Default case in switch maps to "Grátis"
        var cut = RenderComponent<PlanBadge>(parameters => parameters
            .Add(p => p.Plan, "Unknown"));

        cut.Markup.Should().Contain("Grátis");
    }
}
