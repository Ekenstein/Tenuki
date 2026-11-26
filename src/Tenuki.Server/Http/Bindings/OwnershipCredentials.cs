using System.Globalization;
using System.Net.Http.Headers;

namespace Tenuki.Server.Http.Bindings;

public sealed record OwnershipCredentials(SecretToken Token)
{
    public static ValueTask<OwnershipCredentials?> BindAsync(HttpContext context)
    {
        if (
            !AuthenticationHeaderValue.TryParse(
                context.Request.Headers.Authorization.ToString(),
                out var header
            )
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || !SecretToken.TryParse(header.Parameter, CultureInfo.InvariantCulture, out var token)
        )
        {
            return ValueTask.FromResult<OwnershipCredentials?>(null);
        }

        return ValueTask.FromResult<OwnershipCredentials?>(new OwnershipCredentials(token));
    }
}
