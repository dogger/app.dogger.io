using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;

namespace Dogger.Infrastructure.Auth.Auth0
{
    public interface IManagementApiClient : IDisposable
    {
        Task<User> CreateUserAsync(UserCreateRequest userCreateRequest);
        Task LinkUserAccountAsync(string userId, UserAccountLinkRequest userAccountLinkRequest);
        Task DeleteUserAsync(string userId);
        Task<IList<User>> GetUsersByEmailAsync(string email);
        Task<IList<User>> GetAllUsersAsync(GetUsersRequest getUsersRequest, PaginationInfo paginationInfo);
    }
}
