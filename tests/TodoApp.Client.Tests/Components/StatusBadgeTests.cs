using Bunit;
using TodoApp.Client.Components;
using TodoApp.Shared.Models;

namespace TodoApp.Client.Tests.Components;

public class StatusBadgeTests : TestContext
{
    [Fact]
    public void NotStarted_RendersGrayBadge_WithText()
    {
        var cut = RenderComponent<StatusBadge>(parameters =>
            parameters.Add(p => p.Status, TodoStatus.NotStarted));

        var badge = cut.Find(".badge");
        Assert.Contains("未着手", badge.TextContent);
        Assert.Contains("bg-secondary", badge.ClassList);
    }

    [Fact]
    public void InProgress_RendersBlue_Badge_WithText()
    {
        var cut = RenderComponent<StatusBadge>(parameters =>
            parameters.Add(p => p.Status, TodoStatus.InProgress));

        var badge = cut.Find(".badge");
        Assert.Contains("進行中", badge.TextContent);
        Assert.Contains("bg-primary", badge.ClassList);
    }

    [Fact]
    public void Completed_RendersGreenBadge_WithText()
    {
        var cut = RenderComponent<StatusBadge>(parameters =>
            parameters.Add(p => p.Status, TodoStatus.Completed));

        var badge = cut.Find(".badge");
        Assert.Contains("完了", badge.TextContent);
        Assert.Contains("bg-success", badge.ClassList);
    }
}
