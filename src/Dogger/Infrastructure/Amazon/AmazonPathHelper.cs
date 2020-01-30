using System;

namespace Dogger.Infrastructure.Amazon
{
    public static class AmazonPathHelper
    {
        public static string GetUserPath(Guid? userId)
        {
            return userId != null ? 
                $"/customers/{userId}/" : 
                "/";
        }
    }
}
