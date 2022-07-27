using Grocy.RestAPI.Models;

namespace Grocy.RestAPI;

public interface IGenericEntityAPI
{
    Task<IEnumerable<Entity>> GetEntities();
}