using System.ComponentModel.DataAnnotations;

namespace Tenuki.Services.Application;

public sealed record HmacTokenOptions
{
    [Required]
    public required string Secret { get; init; }
}
