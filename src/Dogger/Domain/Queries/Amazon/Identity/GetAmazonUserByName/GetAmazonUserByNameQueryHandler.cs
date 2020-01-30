using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.Amazon.Identity.GetAmazonUserByName
{
    public class GetAmazonUserByNameQueryHandler : IRequestHandler<GetAmazonUserByNameQuery, AmazonUser>
    {
        private readonly DataContext dataContext;

        public GetAmazonUserByNameQueryHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<AmazonUser> Handle(GetAmazonUserByNameQuery request, CancellationToken cancellationToken)
        {
            var amazonUser = await this.dataContext.AmazonUsers
                .Where(x => x.Name == request.Name)
                .SingleOrDefaultAsync(cancellationToken);
            return amazonUser;
        }
    }
}
