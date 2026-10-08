using System.ComponentModel.DataAnnotations;

namespace Pikwise.Application.RequirementParsing.DTOs;

public sealed class NaturalLanguageRequestDto
{
    public const int MaxTextLength = 1000;

    // Required also rejects whitespace-only text.
    [Required, MaxLength(MaxTextLength)]
    public string? Text { get; init; }
}
