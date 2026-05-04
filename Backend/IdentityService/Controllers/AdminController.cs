using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("admin")]
public sealed class AdminController(IIdentityService identityService) : ControllerBase
{
    [HttpPost("create-claims-specialist")]
    public Task<ActionResult<UserResponse>> CreateClaimsSpecialist(CreateUserRequest request, CancellationToken cancellationToken)
    {
        return CreateForRole(request, UserRole.ClaimsSpecialist, cancellationToken);
    }
    [HttpPost("create-support-specialist")]
    public Task<ActionResult<UserResponse>> CreateSupportSpecialist(CreateUserRequest request, CancellationToken cancellationToken)
    {
        return CreateForRole(request, UserRole.SupportSpecialist, cancellationToken);
    }
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await identityService.GetUsersAsync(cancellationToken));
    }
    [HttpPatch("users/{userId:guid}/status")]
    public async Task<ActionResult<UserResponse>> UpdateStatus(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await identityService.UpdateUserStatusAsync(userId, request.IsActive, cancellationToken));
    }
    private async Task<ActionResult<UserResponse>> CreateForRole(CreateUserRequest request, UserRole role, CancellationToken cancellationToken)
    {
        return Ok(await identityService.CreateUserAsync(request, role, cancellationToken));
    }
}
