using System.ComponentModel.DataAnnotations;

namespace Pikwise.Application.Products.Validators;

[AttributeUsage(AttributeTargets.Property)]
// Reject excess decimal places before SQL Server can round the submitted value.
public sealed class DecimalScaleAttribute(int scale) : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is decimal number && decimal.Round(number, scale) == number;

    public override string FormatErrorMessage(string name) =>
        $"{name} must have at most {scale} decimal places.";
}
