namespace Lagedra.SharedKernel.Domain;

/// <summary>
/// Whether a host is verifying as a person or as a property-management business.
/// Individuals complete government-ID KYC. Companies submit business registration
/// documents and are never asked for a personal ID.
/// </summary>
public enum HostKind
{
    Individual = 0,
    PropertyManagementCompany = 1,
}
