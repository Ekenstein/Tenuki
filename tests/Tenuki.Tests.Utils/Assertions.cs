using Xunit.Sdk;

namespace Tenuki.Tests.Utils;

public static class Assertions
{
    public static T AssertNoFailure<T>(this Result<T> result) =>
        result switch
        {
            Success<T> success => success.Value,
            Failure failure => throw FailException.ForFailure(
                $"Expected Success, but got Failure with errors: {string.Join(", ", failure.Errors.Select(x => x.Code))}"
            ),
        };

    public static Task<T> AssertNoFailureAsync<T>(
        this Task<Result<T>> resultTask,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return resultTask.ContinueWith(
            task =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return task.Result.AssertNoFailure();
            },
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }
}
