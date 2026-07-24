using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

namespace ScreenToImageConverter.Tests.Unit;

/// <summary>
/// Unit tests for HtmlRequestValidator.
/// Comprehensive coverage of all validation rules and edge cases.
/// </summary>
public class HtmlRequestValidatorTests
{
    #region Null Command Tests

    [Fact]
    public void Validate_WithNullCommand_ShouldReturnError()
    {
        // Act
        var errors = HtmlRequestValidator.Validate(null!);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("Command cannot be null", errors);
    }

    [Fact]
    public void IsValid_WithNullCommand_ShouldReturnFalse()
    {
        // Act
        var isValid = HtmlRequestValidator.IsValid(null!);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void TryValidate_WithNullCommand_ShouldReturnFalseAndErrors()
    {
        // Act
        var result = HtmlRequestValidator.TryValidate(null!, out var errors);

        // Assert
        Assert.False(result);
        Assert.NotEmpty(errors);
    }

    #endregion

    #region RequestId Validation

    [Fact]
    public void Validate_WithNullRequestId_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = null,
            Url = "https://example.com"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("RequestId is required"));
    }

    [Fact]
    public void Validate_WithEmptyRequestId_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = string.Empty,
            Url = "https://example.com"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("RequestId is required"));
    }

    [Fact]
    public void Validate_WithWhitespaceRequestId_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "   ",
            Url = "https://example.com"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("RequestId is required"));
    }

    #endregion

    #region URL Validation

    [Fact]
    public void Validate_WithNullUrl_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = null
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Url is required"));
    }

    [Fact]
    public void Validate_WithEmptyUrl_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = string.Empty
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Url is required"));
    }

    [Fact]
    public void Validate_WithWhitespaceUrl_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "   "
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Url is required"));
    }

    [Fact]
    public void Validate_WithInvalidUrl_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "not-a-valid-url"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Url must be a valid HTTP or HTTPS URL"));
    }

    [Fact]
    public void Validate_WithFtpUrl_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "ftp://example.com/file.txt"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Url must be a valid HTTP or HTTPS URL"));
    }

    [Fact]
    public void Validate_WithFileUrl_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "file:///C:/test.html"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Url must be a valid HTTP or HTTPS URL"));
    }

    [Fact]
    public void Validate_WithHttpUrl_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "http://example.com"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithHttpsUrl_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithComplexHttpsUrl_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://sub.example.com:8443/path?query=value&other=123#fragment"
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region ViewportWidth Validation

    [Fact]
    public void Validate_WithNullViewportWidth_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = null
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithZeroViewportWidth_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = 0
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("ViewportWidth must be greater than 0"));
    }

    [Fact]
    public void Validate_WithNegativeViewportWidth_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = -100
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("ViewportWidth must be greater than 0"));
    }

    [Fact]
    public void Validate_WithValidViewportWidth_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = 1920
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithMinimalViewportWidth_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportWidth = 1
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region ViewportHeight Validation

    [Fact]
    public void Validate_WithNullViewportHeight_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportHeight = null
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithZeroViewportHeight_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportHeight = 0
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("ViewportHeight must be greater than 0"));
    }

    [Fact]
    public void Validate_WithNegativeViewportHeight_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportHeight = -100
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("ViewportHeight must be greater than 0"));
    }

    [Fact]
    public void Validate_WithValidViewportHeight_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            ViewportHeight = 1080
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region TimeoutMs Validation

    [Fact]
    public void Validate_WithNullTimeoutMs_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            TimeoutMs = null
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WithZeroTimeoutMs_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            TimeoutMs = 0
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("TimeoutMs must be greater than 0"));
    }

    [Fact]
    public void Validate_WithNegativeTimeoutMs_ShouldReturnError()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            TimeoutMs = -5000
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("TimeoutMs must be greater than 0"));
    }

    [Fact]
    public void Validate_WithValidTimeoutMs_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-123",
            Url = "https://example.com",
            TimeoutMs = 30000
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
    }

    #endregion

    #region Multiple Errors

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "",
            Url = "not-a-url",
            ViewportWidth = 0,
            ViewportHeight = -100,
            TimeoutMs = 0
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.NotEmpty(errors);
        Assert.True(errors.Count >= 5, $"Expected at least 5 errors but got {errors.Count}");
    }

    #endregion

    #region Valid Complete Command

    [Fact]
    public void Validate_WithCompleteValidCommand_ShouldBeValid()
    {
        // Arrange
        var command = new ConvertHtmlToImageCommand
        {
            RequestId = "req-12345",
            Url = "https://www.example.com",
            ViewportWidth = 1920,
            ViewportHeight = 1080,
            TimeoutMs = 30000,
            SourceId = "source-1",
            CorrelationId = "corr-1",
            WaitForPageLoad = true
        };

        // Act
        var errors = HtmlRequestValidator.Validate(command);

        // Assert
        Assert.Empty(errors);
        Assert.True(HtmlRequestValidator.IsValid(command));
        Assert.True(HtmlRequestValidator.TryValidate(command, out var validationErrors));
        Assert.Empty(validationErrors);
    }

    #endregion
}
