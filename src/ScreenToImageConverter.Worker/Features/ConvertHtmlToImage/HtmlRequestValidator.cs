using ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

namespace ScreenToImageConverter.Worker.Features.ConvertHtmlToImage;

/// <summary>
/// Validates ConvertHtmlToImageCommand requests.
/// Ensures input data meets required constraints.
/// </summary>
public class HtmlRequestValidator
{
    /// <summary>
    /// Validates a conversion command.
    /// </summary>
    /// <returns>Empty collection if valid, otherwise contains validation error messages.</returns>
    public static ICollection<string> Validate(ConvertHtmlToImageCommand command)
    {
        var errors = new List<string>();

        if (command == null)
        {
            errors.Add("Command cannot be null");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(command.RequestId))
            errors.Add("RequestId is required.");

        if (string.IsNullOrWhiteSpace(command.Url))
            errors.Add("Url is required.");
        else if (!Uri.TryCreate(command.Url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            errors.Add("Url must be a valid HTTP or HTTPS URL.");

        if (command.ViewportWidth.HasValue && command.ViewportWidth <= 0)
            errors.Add("ViewportWidth must be greater than 0.");

        if (command.ViewportHeight.HasValue && command.ViewportHeight <= 0)
            errors.Add("ViewportHeight must be greater than 0.");

        if (command.TimeoutMs.HasValue && command.TimeoutMs <= 0)
            errors.Add("TimeoutMs must be greater than 0.");

        return errors;
    }

    /// <summary>
    /// Checks if a command is valid.
    /// </summary>
    public static bool IsValid(ConvertHtmlToImageCommand command)
    {
        return Validate(command).Count == 0;
    }

    /// <summary>
    /// Tries to validate a command and returns validation errors if any.
    /// </summary>
    public static bool TryValidate(ConvertHtmlToImageCommand command, out ICollection<string> errors)
    {
        errors = Validate(command);
        return errors.Count == 0;
    }
}
