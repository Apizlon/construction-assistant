using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;
using UserService.Application.Models;

namespace UserService.DataAccess.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(UserDbContext dbContext, ILogger<UserRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return _dbContext.Users.AnyAsync(u => u.Email == email);
    }

    public async Task CreateAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("User created in database - {UserId}", user.Id);
    }
}

