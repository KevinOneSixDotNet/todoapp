using System.ComponentModel.DataAnnotations;

namespace FunctionTodo.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotDefaultDateAttribute : ValidationAttribute
{
    public NotDefaultDateAttribute()
        : base("A valid due date is required.") { }

    public override bool IsValid(object? value) =>
        value is DateTime dt && dt != default;
}
