using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IUserManagementService
    {
        Task<UserDto> GetUserAsync(Guid userId);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<IEnumerable<UserDto>> GetUsersByTypeAsync(UserType userType);
        Task<UserDto> CreateUserAsync(CreateUserDto dto, string createdBy);
        Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto, string updatedBy);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<bool> ToggleUserStatusAsync(Guid userId);
        Task<bool> AssignRoleAsync(Guid userId, string role);
        Task<bool> RemoveRoleAsync(Guid userId, string role);
        Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);
    }

    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserManagementService(
            UserManager<User> userManager,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<UserDto> GetUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            return await MapUserToDtoAsync(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                userDtos.Add(await MapUserToDtoAsync(user));
            }

            return userDtos;
        }

        public async Task<IEnumerable<UserDto>> GetUsersByTypeAsync(UserType userType)
        {
            var users = await _unitOfWork.Users.GetUsersByTypeAsync(userType);
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                userDtos.Add(await MapUserToDtoAsync(user));
            }

            return userDtos;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto, string createdBy)
        {
            // Check if email exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new ValidationException("Email is already registered.");
            }

            // Create user
            var user = new User
            {
                Email = dto.Email,
                UserName = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                UserType = dto.UserType,
                IsActive = true,
                CreatedBy = createdBy
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ValidationException($"User creation failed: {errors}");
            }

            // Assign roles
            if (dto.Roles != null && dto.Roles.Any())
            {
                foreach (var role in dto.Roles)
                {
                    if (await _userManager.IsInRoleAsync(user, role))
                        continue;

                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            return await MapUserToDtoAsync(user);
        }

        public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto, string updatedBy)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            // Update fields
            if (!string.IsNullOrEmpty(dto.FullName))
                user.FullName = dto.FullName;

            if (!string.IsNullOrEmpty(dto.PhoneNumber))
                user.PhoneNumber = dto.PhoneNumber;

            if (dto.UserType.HasValue)
                user.UserType = dto.UserType.Value;

            if (dto.IsActive.HasValue)
                user.IsActive = dto.IsActive.Value;

            user.UpdatedBy = updatedBy;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ValidationException($"User update failed: {errors}");
            }

            // Update roles if provided
            if (dto.Roles != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remove roles not in new list
                foreach (var role in currentRoles)
                {
                    if (!dto.Roles.Contains(role))
                    {
                        await _userManager.RemoveFromRoleAsync(user, role);
                    }
                }

                // Add new roles
                foreach (var role in dto.Roles)
                {
                    if (!currentRoles.Contains(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }
            }

            return await MapUserToDtoAsync(user);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            // Soft delete
            user.IsDeleted = true;
            user.IsActive = false;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleUserStatusAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            user.IsActive = !user.IsActive;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return user.IsActive;
        }

        public async Task<bool> AssignRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            if (await _userManager.IsInRoleAsync(user, role))
            {
                throw new ValidationException($"User already has role '{role}'.");
            }

            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ValidationException($"Failed to assign role: {errors}");
            }

            return true;
        }

        public async Task<bool> RemoveRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                throw new ValidationException($"User does not have role '{role}'.");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, role);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ValidationException($"Failed to remove role: {errors}");
            }

            return true;
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            return await _userManager.GetRolesAsync(user);
        }

        private async Task<UserDto> MapUserToDtoAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType.ToString(),
                IsActive = user.IsActive,
                LastLogin = user.LastLogin,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }

    public class CreateUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserType UserType { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class UpdateUserDto
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public UserType? UserType { get; set; }
        public bool? IsActive { get; set; }
        public List<string>? Roles { get; set; }
    }
}
