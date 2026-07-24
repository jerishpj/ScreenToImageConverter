using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

namespace ScreenToImageConverter.Tests.Unit;

/// <summary>
/// Edge case and boundary condition tests.
/// Tests cover unusual input values, extreme scenarios, and boundary conditions.
/// </summary>
public class EdgeCaseAndBoundaryTests
{
    #region URL Edge Cases

    [Theory]
    [InlineData("http://localhost/")]
    [InlineData("https://127.0.0.1/")]
    [InlineData("http://[::1]/")]
    [InlineData("https://subdomain.example.co.uk/")]
    [InlineData("https://example.com:8080/path")]
    public void Validator_WithVariousValidUrls_ShouldPassValidation(string url)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = url
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("http://")]
    [InlineData("https://")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///path")]
    public void Validator_WithInvalidUrls_ShouldFailValidation(string url)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = url
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
    }

    #endregion

    #region Viewport Dimension Edge Cases

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1920)]
    [InlineData(4096)]
    public void Validator_WithValidViewportWidths_ShouldPass(int width)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = width
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Validator_WithInvalidViewportWidths_ShouldFail(int width)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = width
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("ViewportWidth must be greater than 0"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(768)]
    [InlineData(1080)]
    [InlineData(2160)]
    public void Validator_WithValidViewportHeights_ShouldPass(int height)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportHeight = height
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-2000)]
    public void Validator_WithInvalidViewportHeights_ShouldFail(int height)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportHeight = height
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("ViewportHeight must be greater than 0"));
    }

    #endregion

    #region Timeout Edge Cases

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(5000)]
    [InlineData(60000)]
    public void Validator_WithValidTimeouts_ShouldPass(int timeoutMs)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            TimeoutMs = timeoutMs
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5000)]
    public void Validator_WithInvalidTimeouts_ShouldFail(int timeoutMs)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            TimeoutMs = timeoutMs
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("TimeoutMs must be greater than 0"));
    }

    #endregion

    #region RequestId Edge Cases

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Validator_WithWhitespaceOrEmptyRequestId_ShouldFail(string requestId)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = requestId,
            Url = "https://example.com"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
    }

    [Theory]
    [InlineData("req-123")]
    [InlineData("12345")]
    [InlineData("aaaa-bbbb-cccc-dddd")]
    [InlineData("a")]
    [InlineData("VeryLongRequestIdWith1234567890WithMoreCharactersForTesting")]
    public void Validator_WithValidRequestIds_ShouldPass(string requestId)
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = requestId,
            Url = "https://example.com"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region Command Property Edge Cases

    [Fact]
    public void Command_WithAllPropertiesNull_ShouldInitialize()
    {
        // Act
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = null!,
            Url = null!,
            ViewportWidth = null,
            ViewportHeight = null,
            TimeoutMs = null,
            SourceId = null,
            CorrelationId = null,
            WaitForPageLoad = null,
            ScreenshotName = null
        };

        // Assert
        Assert.Null(command.RequestId);
        Assert.Null(command.Url);
        Assert.Null(command.ViewportWidth);
    }

    [Fact]
    public void Command_WithEmptyStringProperties_ShouldInitialize()
    {
        // Act
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "",
            Url = "",
            SourceId = "",
            CorrelationId = "",
            ScreenshotName = ""
        };

        // Assert
        Assert.Empty(command.RequestId);
        Assert.Empty(command.Url);
        Assert.Empty(command.SourceId);
    }

    [Fact]
    public void Command_WithSpecialCharactersInStrings_ShouldPreserve()
    {
        // Arrange
        var specialUrl = "https://example.com/path?query=value&special=@#$%^&*()";
        var specialSource = "source@#$%^&*()";
        var specialCorr = "corr-!@#$%^&*()";

        // Act
        var command = new ConvertHtmlToImageCommand
        {
            Url = specialUrl,
            SourceId = specialSource,
            CorrelationId = specialCorr
        };

        // Assert
        Assert.Equal(specialUrl, command.Url);
        Assert.Equal(specialSource, command.SourceId);
        Assert.Equal(specialCorr, command.CorrelationId);
    }

    [Fact]
    public void Command_WithUnicodeCharacters_ShouldPreserve()
    {
        // Arrange
        var unicodeScreenshotName = "Screenshot-日本語-العربية-Ελληνικά";

        // Act
        var command = new ConvertHtmlToImageCommand
        {
            ScreenshotName = unicodeScreenshotName
        };

        // Assert
        Assert.Equal(unicodeScreenshotName, command.ScreenshotName);
    }

    #endregion

    #region Response Edge Cases

    [Fact]
    public void Response_WithExtremeLargeFileSize_ShouldPreserve()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "large.png",
            "screenshots",
            "https://storage.azure.com/screenshots/large.png",
            fileSizeBytes: long.MaxValue - 1);

        // Assert
        Assert.Equal(long.MaxValue - 1, response.FileSizeBytes);
    }

    [Fact]
    public void Response_WithNegativeFileSize_ShouldPreserve()
    {
        // Act
        var response = new ImageMetadataResponse { FileSizeBytes = -1 };

        // Assert
        Assert.Equal(-1, response.FileSizeBytes);
    }

    [Fact]
    public void Response_WithVeryLargeDuration_ShouldPreserve()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "test.png",
            "screenshots",
            "https://storage.azure.com/screenshots/test.png",
            processingDurationMs: long.MaxValue - 1);

        // Assert
        Assert.Equal(long.MaxValue - 1, response.ProcessingDurationMs);
    }

    [Fact]
    public void Response_WithZeroDuration_ShouldPreserve()
    {
        // Act
        var response = ImageMetadataResponse.CreateSuccess(
            "req-123",
            "https://example.com",
            "test.png",
            "screenshots",
            "https://storage.azure.com/screenshots/test.png",
            processingDurationMs: 0);

        // Assert
        Assert.Equal(0, response.ProcessingDurationMs);
    }

    [Fact]
    public void Response_WithEmptyErrorMessage_ShouldPreserve()
    {
        // Act
        var response = ImageMetadataResponse.CreateFailure(
            "req-123",
            "https://example.com",
            "");

        // Assert
        Assert.Empty(response.ErrorMessage);
    }

    [Fact]
    public void Response_WithVeryLongErrorMessage_ShouldPreserve()
    {
        // Arrange
        var longError = new string('x', 10000);

        // Act
        var response = ImageMetadataResponse.CreateFailure(
            "req-123",
            "https://example.com",
            longError);

        // Assert
        Assert.Equal(longError, response.ErrorMessage);
    }

    #endregion

    #region Multiple Validation Errors

    [Fact]
    public void Validator_WithAllValidationErrorsSimultaneously_ShouldReturnAll()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "",
            Url = "",
            ViewportWidth = 0,
            ViewportHeight = -100,
            TimeoutMs = -5000
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.True(errors.Count >= 5);
        Assert.Contains(errors, e => e.Contains("RequestId is required"));
        Assert.Contains(errors, e => e.Contains("Url is required"));
        Assert.Contains(errors, e => e.Contains("ViewportWidth must be greater than 0"));
        Assert.Contains(errors, e => e.Contains("ViewportHeight must be greater than 0"));
        Assert.Contains(errors, e => e.Contains("TimeoutMs must be greater than 0"));
    }

    #endregion

    #region Special String Cases

    [Theory]
    [InlineData("https://example.com/path\nwith\nnewlines")]
    [InlineData("https://example.com/path\twith\ttabs")]
    [InlineData("https://example.com/path with spaces")]
    public void Command_WithSpecialNewlineTabSpaceCharacters_ShouldPreserve(string url)
    {
        // Act
        var command = new ConvertHtmlToImageCommand { Url = url };

        // Assert
        Assert.Equal(url, command.Url);
    }

    #endregion

    #region Null Tolerance Tests

    [Fact]
    public void Validator_WithNullCommandAndOtherProperties_ShouldReturn()
    {
        // Act
        var errors = HtmlRequestValidator.Validate(null!);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Single(errors);
        Assert.Contains("null", errors.First().ToLower());
    }

    [Fact]
    public void IsValid_WithNullCommand_ShouldReturnFalse()
    {
        // Act
        var result = HtmlRequestValidator.IsValid(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryValidate_WithNullCommand_ShouldReturnFalseWithErrors()
    {
        // Act
        var result = HtmlRequestValidator.TryValidate(null!, out var errors);

        // Assert
        Assert.False(result);
        Assert.NotEmpty(errors);
    }

    #endregion
}
