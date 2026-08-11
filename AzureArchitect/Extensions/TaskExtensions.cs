using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServices.Extensions
{
    public static class TaskExtensions
    {
        public static async Task IgnoreCancellationAsync(this Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // expected for cancelled durable timers — swallow
            }
        }

        public static async Task<T?> IgnoreCancellationAsync<T>(this Task<T> task) where T : class
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }
    }
}
