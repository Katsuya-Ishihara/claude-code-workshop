using Bunit;
using TodoApp.Client.Components;

namespace TodoApp.Client.Tests.Components;

public class ConfirmDialogTests : TestContext
{
    [Fact]
    public void WhenIsVisibleFalse_DialogIsHidden()
    {
        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.Title, "確認")
            .Add(p => p.Message, "削除しますか？"));

        Assert.Empty(cut.FindAll(".confirm-dialog-overlay"));
    }

    [Fact]
    public void WhenIsVisibleTrue_DialogIsShown_WithTitleAndMessage()
    {
        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "確認")
            .Add(p => p.Message, "削除しますか？"));

        var overlay = cut.Find(".confirm-dialog-overlay");
        Assert.NotNull(overlay);
        Assert.Contains("確認", cut.Find(".confirm-dialog-title").TextContent);
        Assert.Contains("削除しますか？", cut.Find(".confirm-dialog-message").TextContent);
    }

    [Fact]
    public void ClickConfirmButton_InvokesOnConfirmWithTrue()
    {
        bool? result = null;
        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "確認")
            .Add(p => p.Message, "削除しますか？")
            .Add(p => p.OnConfirm, (bool value) => { result = value; }));

        cut.Find(".btn-confirm").Click();
        Assert.True(result);
    }

    [Fact]
    public void ClickCancelButton_InvokesOnConfirmWithFalse()
    {
        bool? result = null;
        var cut = RenderComponent<ConfirmDialog>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "確認")
            .Add(p => p.Message, "削除しますか？")
            .Add(p => p.OnConfirm, (bool value) => { result = value; }));

        cut.Find(".btn-cancel").Click();
        Assert.False(result);
    }
}
