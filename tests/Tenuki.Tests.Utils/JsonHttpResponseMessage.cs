using System.Net;

namespace Tenuki.Tests.Utils;

public sealed class JsonHttpResponseMessage<T>(HttpResponseMessage response) : IDisposable
{
    private readonly Lazy<Func<CancellationToken, Task<T>>> _lazyResult = new(
        async cancellationToken =>
        {
            return await response.Content.ReadFromJsonAsync<T>(
                    JsonUtils.DefaultOptions,
                    cancellationToken
                )
                ?? throw new InvalidOperationException(
                    $"Failed to deserialize the http response message to '{typeof(T).Name}'"
                );
        }
    );

    private readonly Lazy<Func<CancellationToken, Task<IEnumerable<Error>>>> _lazyErrors = new(
        async cancellationToken =>
        {
            return await response.Content.ReadFromJsonAsync<IEnumerable<Error>>(
                    JsonUtils.DefaultOptions,
                    cancellationToken
                )
                ?? throw new InvalidOperationException(
                    $"Failed to deserialize the http response message to '{typeof(IEnumerable<Error>).Name}'"
                );
        }
    );

    public Task<T> GetValueAsync(CancellationToken cancellationToken) =>
        _lazyResult.Value(cancellationToken);

    public Task<IEnumerable<Error>> GetErrorsAsync(CancellationToken cancellationToken) =>
        _lazyErrors.Value(cancellationToken);

    public HttpStatusCode StatusCode => response.StatusCode;
    public HttpContent Content => response.Content;

    public void Dispose() => response.Dispose();
}
