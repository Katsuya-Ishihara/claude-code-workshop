using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Client.Components;
using TodoApp.Client.Services;

namespace TodoApp.Client.Tests.Components;

public class ToastTests : TestContext
{
    [Fact]
    public void WhenNoToast_NothingRendered()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        Services.AddSingleton<IToastService>(toastService);
        var cut = RenderComponent<Toast>();

        Assert.Empty(cut.FindAll(".toast-container .toast"));
    }

    [Fact]
    public void ShowSuccess_RendersSuccessToast()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        Services.AddSingleton<IToastService>(toastService);
        var cut = RenderComponent<Toast>();

        cut.InvokeAsync(() => toastService.ShowSuccess("保存しました"));

        var toast = cut.Find(".toast");
        Assert.Contains("保存しました", toast.TextContent);
        Assert.Contains("toast-success", toast.ClassList);
    }

    [Fact]
    public void ShowError_RendersErrorToast()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        Services.AddSingleton<IToastService>(toastService);
        var cut = RenderComponent<Toast>();

        cut.InvokeAsync(() => toastService.ShowError("エラーが発生しました"));

        var toast = cut.Find(".toast");
        Assert.Contains("エラーが発生しました", toast.TextContent);
        Assert.Contains("toast-error", toast.ClassList);
    }

    [Fact]
    public void ShowInfo_RendersInfoToast()
    {
        var toastService = new ToastService();
        Services.AddSingleton(toastService);
        Services.AddSingleton<IToastService>(toastService);
        var cut = RenderComponent<Toast>();

        cut.InvokeAsync(() => toastService.ShowInfo("情報メッセージ"));

        var toast = cut.Find(".toast");
        Assert.Contains("情報メッセージ", toast.TextContent);
        Assert.Contains("toast-info", toast.ClassList);
    }
}
