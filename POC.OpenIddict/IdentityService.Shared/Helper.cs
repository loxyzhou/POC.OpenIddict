using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityService.Shared
{
    public static class Helper
    {
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            var results = new List<T>();
            await foreach (var item in items.WithCancellation(cancellationToken)
                                            .ConfigureAwait(false))
                results.Add(item);
            return results;
        }
    }
}
