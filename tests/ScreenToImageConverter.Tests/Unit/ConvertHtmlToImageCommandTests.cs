using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

namespace ScreenToImageConverter.Tests.Unit;

/// <summary>
/// Unit tests for ConvertHtmlToImageCommand.
/// Tests cover construction, property initialization, and default values.
/// </summary>
public class ConvertHtmlToImageCommandTests
{
    #region Constructor and Default Values Tests

    [Fact]
    public void Constructor_WithNoParameters_ShouldSetDefaults()
    {
        // Act
        var command = new ConvertHtmlToImageCommand();

        // Assert
        Assert.Equal(string.Empty, command.Url);
        Assert.Null(command.ViewportWidth);
        Assert.Null(command.ViewportHeight);
        Assert.Null(command.TimeoutMs);
        Assert.Null(command.SourceId);
        Assert.Null(command.CorrelationId);
        Assert.Null(command.WaitForPageLoad);
        Assert.Null(command.ScreenshotName);
        Assert.NotNull(command.RequestId);
        Assert.NotEmpty(command.RequestId);
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueRequestIds()
    {
        // Act
        var command1 = new ConvertHtmlToImageCommand();
        var command2 = new ConvertHtmlToImageCommand();

        // Assert
        Assert.NotEqual(command1.RequestId, command2.RequestId);
    }

    [Fact]
    public void Constructor_ShouldGenerateGuidRequestIds()
    {
        // Act
        var command = new ConvertHtmlToImageCommand();

        // Assert
        Assert.True(Guid.TryParse(command.RequestId, out _));
    }

    #endregion

    #region URL Property Tests

    [Fact]
    public void Url_ShouldBeAssignable()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.Url = "https://example.com";

        // Assert
        Assert.Equal("https://example.com", command.Url);
    }

    [Fact]
    public void Url_ShouldAcceptEmptyString()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.Url = "";

        // Assert
        Assert.Empty(command.Url);
    }

    [Fact]
    public void Url_ShouldAcceptNull()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            Url = "https://example.com"
        };

        // Act
        command.Url = null!;

        // Assert
        Assert.Null(command.Url);
    }

    [Fact]
    public void Url_ShouldAcceptComplexUrls()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();
        var complexUrl = "https://sub.example.com:8443/path?query=value&other=123#fragment";

        // Act
        command.Url = complexUrl;

        // Assert
        Assert.Equal(complexUrl, command.Url);
    }

    #endregion

    #region ViewportWidth Property Tests

    [Fact]
    public void ViewportWidth_ShouldDefaultToNull()
    {
        // Act
        var command = new ConvertHtmlToImageCommand();

        // Assert
        Assert.Null(command.ViewportWidth);
    }

    [Fact]
    public void ViewportWidth_ShouldAcceptPositiveValues()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.ViewportWidth = 1920;

        // Assert
        Assert.Equal(1920, command.ViewportWidth);
    }

    [Fact]
    public void ViewportWidth_ShouldAcceptZero()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.ViewportWidth = 0;

        // Assert
        Assert.Equal(0, command.ViewportWidth);
    }

    [Fact]
    public void ViewportWidth_ShouldAcceptNegativeValues()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.ViewportWidth = -100;

        // Assert
        Assert.Equal(-100, command.ViewportWidth);
    }

    [Fact]
    public void ViewportWidth_ShouldAcceptMinAndMaxIntegers()
    {
        // Arrange
        var command1 = new ConvertHtmlToImageCommand();
        var command2 = new ConvertHtmlToImageCommand();

        // Act
        command1.ViewportWidth = int.MinValue;
        command2.ViewportWidth = int.MaxValue;

        // Assert
        Assert.Equal(int.MinValue, command1.ViewportWidth);
        Assert.Equal(int.MaxValue, command2.ViewportWidth);
    }

    #endregion

    #region ViewportHeight Property Tests

    [Fact]
    public void ViewportHeight_ShouldDefaultToNull()
    {
        // Act
        var command = new ConvertHtmlToImageCommand();

        // Assert
        Assert.Null(command.ViewportHeight);
    }

    [Fact]
    public void ViewportHeight_ShouldAcceptPositiveValues()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.ViewportHeight = 1080;

        // Assert
        Assert.Equal(1080, command.ViewportHeight);
    }

    [Fact]
    public void ViewportHeight_ShouldAcceptZero()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.ViewportHeight = 0;

        // Assert
        Assert.Equal(0, command.ViewportHeight);
    }

    #endregion

    #region TimeoutMs Property Tests

    [Fact]
    public void TimeoutMs_ShouldDefaultToNull()
    {
        // Act
        var command = new ConvertHtmlToImageCommand();

        // Assert
        Assert.Null(command.TimeoutMs);
    }

    [Fact]
    public void TimeoutMs_ShouldAcceptPositiveValues()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.TimeoutMs = 30000;

        // Assert
        Assert.Equal(30000, command.TimeoutMs);
    }

    [Fact]
    public void TimeoutMs_ShouldAcceptZero()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.TimeoutMs = 0;

        // Assert
        Assert.Equal(0, command.TimeoutMs);
    }

    #endregion

    #region RequestId Property Tests

    [Fact]
    public void RequestId_ShouldBeAssignable()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.RequestId = "custom-req-123";

        // Assert
        Assert.Equal("custom-req-123", command.RequestId);
    }

    [Fact]
    public void RequestId_ShouldAcceptEmptyString()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.RequestId = "";

        // Assert
        Assert.Empty(command.RequestId);
    }

    [Fact]
    public void RequestId_ShouldAcceptNull()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.RequestId = null!;

        // Assert
        Assert.Null(command.RequestId);
    }

    #endregion

    #region SourceId Property Tests

    [Fact]
    public void SourceId_ShouldDefaultToNull()
    {
        // Act
        var command = new ConvertHtmlToImageCommand();

        // Assert
        Assert.Null(command.SourceId);
    }

    [Fact]
    public void SourceId_ShouldBeAssignable()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.SourceId = "user-123";

        // Assert
        Assert.Equal("user-123", command.SourceId);
    }

    [Fact]
    public void SourceId_ShouldAcceptEmptyString()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.SourceId = "";

        // Assert
        Assert.Empty(command.SourceId);
    }

    #endregion

    #region CorrelationId Property Tests

    [Fact]
    public void CorrelationId_ShouldDefaultToNull()
    {
        // Act
        var command = new ConvertHtmlToImageCommand();

        // Assert
        Assert.Null(command.CorrelationId);
    }

    [Fact]
    public void CorrelationId_ShouldBeAssignable()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.CorrelationId = "corr-123";

        // Assert
        Assert.Equal("corr-123", command.CorrelationId);
    }

    #endregion

    #region WaitForPageLoad Property Tests

    [Fact]
    public void WaitForPageLoad_ShouldDefaultToNull()
    {
        // Act
        var command = new ConvertHtmlToImageCommand();

        // Assert
        Assert.Null(command.WaitForPageLoad);
    }

    [Fact]
    public void WaitForPageLoad_ShouldAcceptTrue()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.WaitForPageLoad = true;

        // Assert
        Assert.True(command.WaitForPageLoad);
    }

    [Fact]
    public void WaitForPageLoad_ShouldAcceptFalse()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.WaitForPageLoad = false;

        // Assert
        Assert.False(command.WaitForPageLoad);
    }

    #endregion

    #region ScreenshotName Property Tests

    [Fact]
    public void ScreenshotName_ShouldDefaultToNull()
    {
        // Act
        var command = new ConvertHtmlToImageCommand();

        // Assert
        Assert.Null(command.ScreenshotName);
    }

    [Fact]
    public void ScreenshotName_ShouldBeAssignable()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.ScreenshotName = "Homepage Screenshot";

        // Assert
        Assert.Equal("Homepage Screenshot", command.ScreenshotName);
    }

    [Fact]
    public void ScreenshotName_ShouldAcceptEmptyString()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand();

        // Act
        command.ScreenshotName = "";

        // Assert
        Assert.Empty(command.ScreenshotName);
    }

    #endregion

    #region Object Initialization Tests

    [Fact]
    public void Command_CanBeInitializedWithObjectInitializer()
    {
        // Act
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            TimeoutMs = 30000,
            SourceId = "source-1",
            CorrelationId = "corr-1",
            WaitForPageLoad = true,
            ScreenshotName = "Test Screenshot"
        };

        // Assert
        Assert.Equal("req-123", command.RequestId);
        Assert.Equal("https://example.com", command.Url);
        Assert.Equal(1920, command.ViewportWidth);
        Assert.Equal(1080, command.ViewportHeight);
        Assert.Equal(30000, command.TimeoutMs);
        Assert.Equal("source-1", command.SourceId);
        Assert.Equal("corr-1", command.CorrelationId);
        Assert.True(command.WaitForPageLoad);
        Assert.Equal("Test Screenshot", command.ScreenshotName);
    }

    [Fact]
    public void Command_ShouldSupportPartialInitialization()
    {
        // Act
        var command = new ConvertHtmlToImageCommand
        {
            Url = "https://example.com",
            ViewportWidth = 1920
        };

        // Assert
        Assert.Equal("https://example.com", command.Url);
        Assert.Equal(1920, command.ViewportWidth);
        Assert.Null(command.ViewportHeight);
        Assert.Null(command.TimeoutMs);
        Assert.Null(command.SourceId);
    }

    #endregion

    #region Independent Property Tests

    [Fact]
    public void CommandProperties_ShouldBeIndependent()
    {
        // Arrange
        var command1 = new ConvertHtmlToImageCommand { Url = "https://example1.com" };
        var command2 = new ConvertHtmlToImageCommand { Url = "https://example2.com" };

        // Assert
        Assert.NotEqual(command1.Url, command2.Url);
        Assert.NotEqual(command1.RequestId, command2.RequestId);
    }

    #endregion
}
