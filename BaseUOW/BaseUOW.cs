using Base.Contracts.DAL;
using Microsoft.EntityFrameworkCore;

namespace BaseUOW;

public class BaseUOW<TDbContext> : IUnitOfWork
    where TDbContext : DbContext
{
    protected readonly TDbContext UOWDbContext;
    public BaseUOW(TDbContext dbContext)
    {
        UOWDbContext = dbContext;
    }
    public async Task<int> SaveChangesAsync()
    {
        return await UOWDbContext.SaveChangesAsync();
    }

    public int SaveChanges()
    {
        return UOWDbContext.SaveChanges();
    }
}