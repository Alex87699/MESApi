using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;

namespace MyApi.Services
{
    public enum UserWriteResult
    {
        Success,
        NotFound,
        EmailConflict
    }

    public interface IUserService
    {
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<User?> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

        Task<(UserWriteResult Result, User? User)> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _db.Users
                .AsNoTracking()
                .OrderBy(user => user.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _db.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
        }

        public async Task<User?> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var email = request.Email.Trim();
            if (await EmailTakenAsync(email, cancellationToken: cancellationToken))
            {
                return null;
            }

            var user = new User
            {
                Name = request.Name.Trim(),
                Email = email,
                Phone = NormalizePhone(request.Phone),
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return user;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return null;
            }
        }

        public async Task<(UserWriteResult Result, User? User)> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (user is null)
            {
                return (UserWriteResult.NotFound, null);
            }

            var email = request.Email.Trim();
            if (await EmailTakenAsync(email, id, cancellationToken))
            {
                return (UserWriteResult.EmailConflict, null);
            }

            user.Name = request.Name.Trim();
            user.Email = email;
            user.Phone = NormalizePhone(request.Phone);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return (UserWriteResult.Success, user);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return (UserWriteResult.EmailConflict, null);
            }
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (user is null)
            {
                return false;
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private Task<bool> EmailTakenAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            return _db.Users.AnyAsync(
                user => user.Id != excludeId && user.Email == email,
                cancellationToken);
        }

        private static string? NormalizePhone(string? phone)
        {
            return string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        }

        private static bool IsUniqueViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException &&
                   (sqlException.Number == 2601 || sqlException.Number == 2627);
        }
    }
}
