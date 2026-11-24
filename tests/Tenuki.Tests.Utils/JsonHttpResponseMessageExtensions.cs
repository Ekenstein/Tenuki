using System.Net;
using Xunit.Sdk;

namespace Tenuki.Tests.Utils;

public static class JsonHttpResponseMessageExtensions
{
    public static async Task<Result<T>> SuccessOrBadRequestAsync<T>(
        this JsonHttpResponseMessage<T> response,
        HttpStatusCode successStatusCode,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (response.StatusCode == successStatusCode)
        {
            return Tenuki.Utils.Result.Success(await response.GetValueAsync(cancellationToken));
        }
        else if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return new Failure([.. await response.GetErrorsAsync(cancellationToken)]);
        }

        throw FailException.ForFailure(
            $"Expected status code {successStatusCode} or BadRequest, but got {response.StatusCode}"
        );
    }

    public static Task<Result<T>> OkOrBadRequestAsync<T>(
        this JsonHttpResponseMessage<T> response,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return response.SuccessOrBadRequestAsync(HttpStatusCode.OK, cancellationToken);
    }
}
