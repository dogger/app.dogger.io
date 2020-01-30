using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.Users.GetUserById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, User>
    {
        private readonly DataContext dataContext;

        public GetUserByIdQueryHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<User> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            return await this.dataContext
                .Users
                .Include(x => x.PullDogSettings)
                .FirstOrDefaultAsync(
                    x => x.Id == request.UserId, 
                    cancellationToken);
        }
    }
}
