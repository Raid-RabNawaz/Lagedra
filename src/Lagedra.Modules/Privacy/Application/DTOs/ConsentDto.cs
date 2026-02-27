using Lagedra.Modules.Privacy.Domain.Enums;

namespace Lagedra.Modules.Privacy.Application.DTOs;

public sealed record ConsentDto(
    ConsentType ConsentType,
    DateTime GrantedAt,
    DateTime? WithdrawnAt,
    string IpAddress,
    string UserAgent);
