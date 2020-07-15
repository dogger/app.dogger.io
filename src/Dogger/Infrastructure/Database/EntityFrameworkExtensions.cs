using Microsoft.Data.SqlClient;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    public static class EntityFrameworkExtensions
    {
        public static bool IsUniqueConstraintViolation(this DbUpdateException exception)
        {
            return
                exception.InnerException is SqlException sqlException &&
                sqlException.Number == 2627;
        }
    }
}
