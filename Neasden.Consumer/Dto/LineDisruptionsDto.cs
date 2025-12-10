using Neasden.Models;

namespace Neasden.Consumer.Dto;

public record LineDisruptionsDto(
    Line Line,
    IEnumerable<DisruptionDto> DisruptionDtos)
{
    public Line Line { init; get; } = Line;
    public IEnumerable<DisruptionDto> DisruptionDtos { init; get; } = DisruptionDtos;
}