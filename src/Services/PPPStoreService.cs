using Dashboard;
using Microsoft.EntityFrameworkCore;
using Dashboard.Services;

namespace Dashboard.Services
{
    public class PPPStoreService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly GeocodingService _geoService;

        public PPPStoreService(IDbContextFactory<AppDbContext> dbFactory, GeocodingService geoService)
        {
            _dbFactory = dbFactory;
            _geoService = geoService;
        }

        public async Task<PPP_Owner?> GetOwnerAsync(int ownerId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            return await db.PPP_Owners.Include(o => o.Address).AsNoTracking().FirstOrDefaultAsync(o => o.Id == ownerId);
        }

        public async Task<List<PPP_Menu>> GetActiveMenusAsync(int ownerId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var today = DateTime.Today.ToUniversalTime();
            return await db.Set<PPP_Menu>().AsNoTracking()
                .Include(m => m.Items).ThenInclude(i => i.Recipe).ThenInclude(r => r.IngredientGroups).ThenInclude(g => g.Ingredients).ThenInclude(ing => ing.Ingredient)
                .Include(m => m.Items).ThenInclude(i => i.Timeframes)
                .Include(m => m.Items).ThenInclude(i => i.Sizes).ThenInclude(s => s.Options)
                .Where(m => m.OwnerId == ownerId && m.IsPublished && m.EndDate >= today)
                .OrderBy(m => m.StartDate).ToListAsync();
        }

        public (double Cals, double Prot, double Fat, double Net) GetMacros(PPP_Recipe? r)
        {
            if (r == null || r.Servings <= 0 || r.IngredientGroups == null) return (0, 0, 0, 0);
            double cals = 0, prot = 0, fat = 0, net = 0;
            foreach (var group in r.IngredientGroups)
            {
                foreach (var mapping in group.Ingredients ?? new List<PPP_RecipeIngredientMapping>())
                {
                    if (mapping.Ingredient != null)
                    {
                        cals += mapping.Ingredient.Calories * mapping.Quantity;
                        prot += mapping.Ingredient.Protein * mapping.Quantity;
                        fat += mapping.Ingredient.Fat * mapping.Quantity;
                        net += (mapping.Ingredient.Carbs - mapping.Ingredient.Fiber) * mapping.Quantity;
                    }
                }
            }
            return (Math.Round(cals / r.Servings, 0), Math.Round(prot / r.Servings, 1), Math.Round(fat / r.Servings, 1), Math.Round(net / r.Servings, 1));
        }

        public string GetSimplifiedIngredients(PPP_Recipe? r)
        {
            if (r == null || r.IngredientGroups == null) return "";
            return string.Join(", ", r.IngredientGroups.SelectMany(g => g.Ingredients ?? new List<PPP_RecipeIngredientMapping>())
                .Where(m => m.Ingredient != null).Select(m => m.Ingredient!.Name.ToLower().Trim()).Distinct());
        }
    }
}