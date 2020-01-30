using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;

namespace Dogger.Infrastructure.Auth.Auth0
{
    [ExcludeFromCodeCoverage]
    public class ManagementApiClientProxy : IManagementApiClient
    {
        private readonly ManagementApiClient managementApiClient;

        public ManagementApiClientProxy(ManagementApiClient managementApiClient)
        {
            this.managementApiClient = managementApiClient;
        }

        public void Dispose()
        {
            this.managementApiClient.Dispose();
        }

        public async Task<User> CreateUserAsync(UserCreateRequest userCreateRequest)
        {
            return await this.managementApiClient.Users.CreateAsync(userCreateRequest);
        }

        public async Task LinkUserAccountAsync(string userId, UserAccountLinkRequest userAccountLinkRequest)
        {
            await this.managementApiClient.Users.LinkAccountAsync(userId, userAccountLinkRequest);
        }

        public async Task DeleteUserAsync(string userId)
        {
            await this.managementApiClient.Users.DeleteAsync(userId);
        }

        public async Task<IList<User>> GetUsersByEmailAsync(string email)
        {
            return await this.managementApiClient.Users.GetUsersByEmailAsync(email);
        }

        public async Task<IList<User>> GetAllUsersAsync(GetUsersRequest getUsersRequest, PaginationInfo paginationInfo)
        {
            return await this.managementApiClient.Users.GetAllAsync(getUsersRequest, paginationInfo);
        }
    }
}
