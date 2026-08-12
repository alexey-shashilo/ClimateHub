namespace ClimateHub.Modules.IAM.Domain;

public interface IUserRepository
{
    Task<UserAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<UserAccount>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(UserAccount user, CancellationToken ct = default);
    Task UpdateAsync(UserAccount user, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<List<Role>> GetUserRolesAsync(Guid userId, CancellationToken ct = default);
}
