using WeeklyUp.WebApp.Components.Shared;

namespace WeeklyUp.WebApp.Tests.Components;

public sealed class ConfirmDialogTests : TestContext
{
    public ConfirmDialogTests()
    {
        Services.AddMudServices(o => o.SnackbarConfiguration.ShowTransitionDuration = 0);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ConfirmDialog_RendersWithContentText()
    {
        // Arrange — ConfirmDialog requer [CascadingParameter] IMudDialogInstance
        // MudDialog renderiza conteúdo via portal — verificar parâmetros no componente
        var mockDialog = Substitute.For<IMudDialogInstance>();

        // Act
        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(p => p.ContentText, "Tem certeza que deseja desconectar?")
            .Add(p => p.ConfirmText, "Desconectar")
            .Add(p => p.CancelText, "Cancelar")
            .AddCascadingValue(mockDialog));

        // Assert — verificar que os parâmetros foram recebidos corretamente
        cut.Instance.ContentText.Should().Be("Tem certeza que deseja desconectar?");
        cut.Instance.ConfirmText.Should().Be("Desconectar");
        cut.Instance.CancelText.Should().Be("Cancelar");
    }

    [Fact]
    public void ConfirmDialog_DefaultTexts_AreSetCorrectly()
    {
        // Arrange
        var mockDialog = Substitute.For<IMudDialogInstance>();

        // Act
        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(p => p.ContentText, "Confirmação necessária")
            .AddCascadingValue(mockDialog));

        // Assert — defaults definidos no componente
        cut.Instance.ConfirmText.Should().Be("Confirmar");
        cut.Instance.CancelText.Should().Be("Cancelar");
        cut.Instance.Color.Should().Be(Color.Error);
    }

}
