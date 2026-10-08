using Pikwise.Application.RequirementParsing.DTOs;

namespace Pikwise.Application.RequirementParsing.Interfaces;

public interface IRequirementParsingService
{
    Task<ParsedRequirementsResponseDto> ParseAsync(NaturalLanguageRequestDto request, CancellationToken cancellationToken = default);
}
