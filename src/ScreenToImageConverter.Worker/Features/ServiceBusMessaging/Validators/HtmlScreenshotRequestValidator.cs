using ScreenToImageConverter.Shared.Messages;

namespace ScreenToImageConverter.Worker.Features.ServiceBusMessaging.Validators;

/// <summary>
/// Validates HtmlScreenshotRequest messages.
/// Part of the ServiceBusMessaging vertical slice.
/// </summary>
public class HtmlScreenshotRequestValidator
{
    /// <summary>
    /// Validates a screenshot request.
    /// </summary>
    /// <returns>Empty collection if valid, otherwise contains validation error messages.</returns>
    public static ICollection<string> Validate(HtmlScreenshotRequest request)
    {
        if (request == null)
        {
            return new[] { "Request cannot be null" };
        }

        return request.Validate();
    }

    /// <summary>
    /// Checks if a request is valid.
    /// </summary>
    public static bool IsValid(HtmlScreenshotRequest request)
    {
        return Validate(request).Count == 0;
    }

    /// <summary>
    /// Tries to validate a request and returns validation errors if any.
    /// </summary>
    public static bool TryValidate(HtmlScreenshotRequest request, out ICollection<string> errors)
    {
        errors = Validate(request);
        return errors.Count == 0;
    }
}
