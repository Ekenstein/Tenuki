using System.Net;
using Xunit.Sdk;

namespace Tenuki.Tests.Utils;

public static class JsonHttpResponseMessageExtensions
{
    public static async Task<Result<T>> OkOrBadRequestAsync<T>(
        this JsonHttpResponseMessage<T> response,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return response.StatusCode switch
        {
            HttpStatusCode.OK => Tenuki.Utils.Result.Success(
                await response.GetValueAsync(cancellationToken)
            ),
            HttpStatusCode.BadRequest => new Failure([
                .. await response.GetErrorsAsync(cancellationToken),
            ]),
            _ => throw FailException.ForFailure(
                $"Expected status code OK or BadRequest, but got {response.StatusCode}"
            ),
        };
    }
}
