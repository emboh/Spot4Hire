using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spot4Hire.Backend.Authorization;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Dtos.Common;
using Spot4Hire.Backend.Dtos.Users;
using Spot4Hire.Backend.Mapping;
using Spot4Hire.Backend.Services.Abstractions;
using Gridify;

namespace Spot4Hire.Backend.Services;

// Admin-only user management, done through UserManager so password hashing,
// normalization, and role changes go through Identity.
public class UserService(UserManager<ApplicationUser> userManager) : IUserService
{
    public async Task<PagedResponse<UserResponse>> GetUsersAsync(GridifyQuery query, CancellationToken ct)
    {
        // Roles are loaded per user after paging, so this does not use the shared helper.
        PagingExtensions.Normalize(query, "email");

        var filtered = userManager.Users.AsNoTracking().ApplyFiltering(query, GridifyMappers.User);
        var totalCount = await filtered.CountAsync(ct);

        var users = await filtered
            .ApplyOrdering(query, GridifyMappers.User)
            .ApplyPaging(query)
            .ToListAsync(ct);

        var items = new List<UserResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            items.Add(user.ToResponse(roles));
        }

        return new PagedResponse<UserResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<UserResponse?> GetUserAsync(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return user.ToResponse(roles);
    }

    public async Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Address = request.Address,
            EmailConfirmed = true,
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Result<UserResponse>.Invalid(Describe(created));
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            return Result<UserResponse>.Invalid(Describe(roleResult));
        }

        var roles = await userManager.GetRolesAsync(user);
        return Result<UserResponse>.Success(user.ToResponse(roles));
    }

    public async Task<Result> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.NotFound();
        }

        // Keep UserName in step with Email; both go through the manager for normalization.
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var email = await userManager.SetEmailAsync(user, request.Email);
            if (!email.Succeeded)
            {
                return Result.Invalid(Describe(email));
            }

            var name = await userManager.SetUserNameAsync(user, request.Email);
            if (!name.Succeeded)
            {
                return Result.Invalid(Describe(name));
            }
        }

        user.Address = request.Address;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Invalid(Describe(update));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(request.Role))
        {
            if (currentRoles.Contains(Roles.Admin) && request.Role != Roles.Admin && await IsLastAdminAsync(user))
            {
                return Result.Invalid("Cannot remove the Admin role from the last remaining admin.");
            }

            var removed = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removed.Succeeded)
            {
                return Result.Invalid(Describe(removed));
            }

            var added = await userManager.AddToRoleAsync(user, request.Role);
            if (!added.Succeeded)
            {
                return Result.Invalid(Describe(added));
            }
        }

        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(Guid id, Guid currentUserId, CancellationToken ct)
    {
        if (id == currentUserId)
        {
            return Result.Forbidden("You cannot delete your own account.");
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.NotFound();
        }

        if (await IsLastAdminAsync(user))
        {
            return Result.Invalid("Cannot delete the last remaining admin.");
        }

        var deleted = await userManager.DeleteAsync(user);
        if (!deleted.Succeeded)
        {
            return Result.Invalid(Describe(deleted));
        }

        return Result.Success();
    }

    public async Task<Result> SetPasswordAsync(Guid id, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.NotFound();
        }

        // Force-set without the old password: issue a reset token and consume it.
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!reset.Succeeded)
        {
            return Result.Invalid(Describe(reset));
        }

        return Result.Success();
    }

    // True when the user is an admin and the only one left.
    private async Task<bool> IsLastAdminAsync(ApplicationUser user)
    {
        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            return false;
        }

        var admins = await userManager.GetUsersInRoleAsync(Roles.Admin);
        return admins.Count <= 1;
    }

    private static string Describe(IdentityResult result)
        => string.Join("; ", result.Errors.Select(e => e.Description));
}
