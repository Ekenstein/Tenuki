namespace Tenuki.Utils;

public interface IIdGenerator
{
    public static readonly IIdGenerator Default = new DefaultIdGenerator();

    Guid Generate();

    private sealed class DefaultIdGenerator : IIdGenerator
    {
        public Guid Generate() => Guid.NewGuid();
    }
}
