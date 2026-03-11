namespace TodoApp.Api.Tests;

public class SanityTest
{
    [Fact]
    public void ProjectReference_SharedModels_AreAccessible()
    {
        var status = TodoApp.Shared.Models.TodoStatus.NotStarted;
        Assert.Equal(0, (int)status);
    }
}
