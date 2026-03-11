using Bunit;
using TodoApp.Client.Components;

namespace TodoApp.Client.Tests.Components;

public class PaginationTests : TestContext
{
    [Fact]
    public void RendersCorrectNumberOfPages()
    {
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5));

        var pageItems = cut.FindAll(".page-item:not(.prev-page):not(.next-page)");
        Assert.Equal(5, pageItems.Count);
    }

    [Fact]
    public void CurrentPage_HasActiveClass()
    {
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 3)
            .Add(p => p.TotalPages, 5));

        var activeItem = cut.Find(".page-item.active");
        Assert.Contains("3", activeItem.TextContent);
    }

    [Fact]
    public void ClickPage_InvokesOnPageChanged()
    {
        int? changedPage = null;
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5)
            .Add(p => p.OnPageChanged, (int page) => { changedPage = page; }));

        var pageLinks = cut.FindAll(".page-item:not(.prev-page):not(.next-page) .page-link");
        pageLinks[2].Click(); // Click page 3

        Assert.Equal(3, changedPage);
    }

    [Fact]
    public void PreviousButton_DisabledOnFirstPage()
    {
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 5));

        var prevItem = cut.Find(".prev-page");
        Assert.Contains("disabled", prevItem.ClassList);
    }

    [Fact]
    public void NextButton_DisabledOnLastPage()
    {
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 5)
            .Add(p => p.TotalPages, 5));

        var nextItem = cut.Find(".next-page");
        Assert.Contains("disabled", nextItem.ClassList);
    }

    [Fact]
    public void SinglePage_DoesNotRender()
    {
        var cut = RenderComponent<Pagination>(parameters => parameters
            .Add(p => p.CurrentPage, 1)
            .Add(p => p.TotalPages, 1));

        Assert.Empty(cut.FindAll(".pagination"));
    }
}
