using Bunit;
using TodoApp.Client.Components;

namespace TodoApp.Client.Tests.Components;

public class SearchBarTests : TestContext
{
    [Fact]
    public void RendersInputAndButton()
    {
        var cut = RenderComponent<SearchBar>();

        Assert.NotNull(cut.Find("input.search-input"));
        Assert.NotNull(cut.Find("button.search-button"));
    }

    [Fact]
    public void ClickSearchButton_InvokesOnSearch_WithInputValue()
    {
        string? searchQuery = null;
        var cut = RenderComponent<SearchBar>(parameters => parameters
            .Add(p => p.OnSearch, (string query) => { searchQuery = query; }));

        cut.Find("input.search-input").Input("テスト検索");
        cut.Find("button.search-button").Click();

        Assert.Equal("テスト検索", searchQuery);
    }

    [Fact]
    public void Placeholder_IsRendered()
    {
        var cut = RenderComponent<SearchBar>(parameters => parameters
            .Add(p => p.Placeholder, "検索..."));

        var input = cut.Find("input.search-input");
        Assert.Equal("検索...", input.GetAttribute("placeholder"));
    }
}
