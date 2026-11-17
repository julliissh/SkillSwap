using Microsoft.EntityFrameworkCore;

namespace Persistence;

public class SkillSwapDbContext : DbContext
{
    public SkillSwapDbContext(DbContextOptions<SkillSwapDbContext> options) : base(options)
    {
    }
}
