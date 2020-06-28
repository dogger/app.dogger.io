using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable CheckNamespace

namespace Dogger.Tests
{
    [ExcludeFromCodeCoverage]
    [DebuggerStepThrough]
    public static class Extensions
    {
        public static async Task ClearCacheAsync(this DataContext dataContext)
        {
            var changedEntriesCopy = dataContext
                .ChangeTracker
                .Entries()
                .ToArray();

            foreach (var entry in changedEntriesCopy)
                await entry.ReloadAsync();
        }

        public static TResponse ToObject<TResponse>(this IActionResult httpResponseMessage) where TResponse : class
        {
            var objectResult = httpResponseMessage as ObjectResult;
            var value = objectResult?.Value as TResponse;
            if (objectResult == null || (value == null && objectResult?.Value != null))
            {
                throw new InvalidOperationException(
                    $"Response of type {objectResult?.Value?.GetType()?.FullName ?? objectResult?.GetType()?.FullName} can't be converted to {typeof(TResponse).FullName}.\nJSON: " + JsonSerializer.Serialize(httpResponseMessage));
            }

            return value;
        }

        public static ValidationProblemDetails GetValidationProblemDetails(this IActionResult httpResponseMessage)
        {
            return httpResponseMessage.ToObject<ValidationProblemDetails>();
        }

        public static int? GetStatusCode(this IActionResult httpResponseMessage)
        {
            var objectResult = httpResponseMessage as IStatusCodeActionResult;
            if (objectResult == null)
                throw new InvalidOperationException($"Can't retrieve status code from this action result of type {httpResponseMessage.GetType().FullName}.\nJSON: " + JsonSerializer.Serialize(httpResponseMessage));

            Debug.Assert(objectResult.StatusCode != null, "objectResult.StatusCode != null");
            return objectResult.StatusCode;
        }

        public static void AssertSuccessfulStatusCode(this IActionResult httpResponseMessage)
        {
            var statusCode = GetStatusCode(httpResponseMessage);
            if (statusCode >= 400)
                throw new InvalidOperationException("Controller call was not successful.\nJSON: " + JsonSerializer.Serialize(httpResponseMessage));
        }

        private static void EnsureControllerContext(this ControllerBase controller)
        {
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
        }

        public static void FakeAuthentication(this ControllerBase controller, string identityName)
        {
            controller.EnsureControllerContext();
            controller.HttpContext.User = TestClaimsPrincipalFactory.CreateWithIdentityName(identityName);
        }
    }
}
