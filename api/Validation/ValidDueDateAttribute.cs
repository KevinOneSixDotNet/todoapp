using System.ComponentModel.DataAnnotations;

namespace FunctionTodo.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ValidDueDateAttribute : ValidationAttribute
{
    private static readonly DateTime Floor = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public ValidDueDateAttribute()
        : base("Due date must be a real date on or after January 1, 2000.") { }

    public override bool IsValid(object? value) =>
        value is DateTime dt && dt != default && dt >= Floor;
}
