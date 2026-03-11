namespace TodoApp.Client.Tests;

public class SanityTest
{
    [Fact]
    public void ProjectReference_SharedModels_AreAccessible()
    {
        var priority = TodoApp.Shared.Models.Priority.High;
        Assert.Equal(2, (int)priority);
    }
}
