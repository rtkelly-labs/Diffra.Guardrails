namespace Diffra.Guardrails.Tests;

public class BuildSkeletonTests
{
    [Fact]
    public void The_test_project_runs()
    {
        // Arrange
        var expected = 2;

        // Act
        var actual = 1 + 1;

        // Assert
        actual.Should().Be(expected, because: "the test runner must execute tests before any rule exists");
    }
}
