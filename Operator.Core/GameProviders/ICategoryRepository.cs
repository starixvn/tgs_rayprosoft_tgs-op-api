using System.Collections.Generic;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;
using TGS.Services.Operator.ApplicationServices.Category;

namespace TGS.Services.Operator.GameProviders;
public interface ICategoryRepository
{

	Task<IEnumerable<Category>> QueryGameByGameTypesAsync(GetCategoriesQuery query, GamePlatformType[] gamePlatformTypes);
}
