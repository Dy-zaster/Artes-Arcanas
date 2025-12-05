using Laa.Content.Core.Mappings;

namespace Laa.Content.Core.Repositories;

public interface IAnimationMappingRepository
{
    AnimationMappingDocument GetMappings();
}
