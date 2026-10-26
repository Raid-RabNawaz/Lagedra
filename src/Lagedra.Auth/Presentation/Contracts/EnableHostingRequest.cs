using Lagedra.SharedKernel.Domain;

namespace Lagedra.Auth.Presentation.Contracts;

public sealed record EnableHostingRequest(
    string City,
    string Country,
    string Bio,
    HostKind HostKind,
    string? State = null,
    string? Languages = null,
    string? Occupation = null,
    string? CompanyName = null);
