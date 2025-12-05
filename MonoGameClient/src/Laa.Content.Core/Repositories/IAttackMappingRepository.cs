using Laa.Content.Core.Mappings;

namespace Laa.Content.Core.Repositories;

public interface IAttackMappingRepository
{
    AttackMappingDocument GetMappings();
}
