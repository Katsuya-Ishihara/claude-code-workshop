using Bunit;
using TodoApp.Client.Components;
using TodoApp.Shared.Models;

namespace TodoApp.Client.Tests.Components;

public class PriorityIconTests : TestContext
{
    [Fact]
    public void Low_RendersDownArrow_InGreen()
    {
        var cut = RenderComponent<PriorityIcon>(parameters =>
            parameters.Add(p => p.Priority, Priority.Low));

        var element = cut.Find(".priority-icon");
        Assert.Contains("↓", element.TextContent);
        Assert.Contains("text-success", element.ClassList);
    }

    [Fact]
    public void Medium_RendersHorizontalLine_InOrange()
    {
        var cut = RenderComponent<PriorityIcon>(parameters =>
            parameters.Add(p => p.Priority, Priority.Medium));

        var element = cut.Find(".priority-icon");
        Assert.Contains("―", element.TextContent);
        Assert.Contains("text-warning", element.ClassList);
    }

    [Fact]
    public void High_RendersUpArrow_InRed()
    {
        var cut = RenderComponent<PriorityIcon>(parameters =>
            parameters.Add(p => p.Priority, Priority.High));

        var element = cut.Find(".priority-icon");
        Assert.Contains("↑", element.TextContent);
        Assert.Contains("text-danger", element.ClassList);
    }
}
