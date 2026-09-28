using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Dtos.Common;
using Gridify;
using Spot4Hire.Backend.Dtos.Users;

namespace Spot4Hire.Backend.Services.Abstractions;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetUsersAsync(GridifyQuery query, CancellationToken ct);

    Task<UserResponse?> GetUserAsync(Guid id, CancellationToken ct);

    Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken ct);

    Task<Result> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct);

    // currentUserId guards against an admin deleting their own account.
    Task<Result> DeleteUserAsync(Guid id, Guid currentUserId, CancellationToken ct);

    // Admin force-set of a user's password.
    Task<Result> SetPasswordAsync(Guid id, string newPassword, CancellationToken ct);
}
