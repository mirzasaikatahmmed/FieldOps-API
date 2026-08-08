using FieldOps.COMMON.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.DAL.Repositories;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
    Task<PasswordResetToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task InvalidateUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _db;

    public PasswordResetTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
        => await _db.PasswordResetTokens.AddAsync(token, cancellationToken);

    public async Task<PasswordResetToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => await _db.PasswordResetTokens
            .IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow, cancellationToken);

    public async Task InvalidateUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _db.PasswordResetTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
            token.UsedAt = DateTime.UtcNow;
    }
}
