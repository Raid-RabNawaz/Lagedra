using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.PartnerNetwork.Domain.Interfaces;

public interface IPartnerOrganizationRepository : IRepository<PartnerOrganization, Guid>
{
}
