using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using ProdmasterProvidersService.Database;

namespace ProdmasterProvidersService.Database.Extensions
{
    public static class UserContextBulkExtensions
    {
        public static Task BulkSaveChangesAsync(this UserContext context, BulkConfig? bulkConfig = null, Action<decimal>? progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            context.FillUpdatedDate();
            return ((UserContext)context).BulkSaveChangesAsync(bulkConfig, progress, cancellationToken);
        }
    }
}
