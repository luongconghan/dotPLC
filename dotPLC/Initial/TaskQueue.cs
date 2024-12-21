using System;
using System.Threading;
using System.Threading.Tasks;

namespace dotPLC.Initial
{
    /// <summary>
    /// Represents first-in, first-out asynchronous operations.
    /// </summary>
    internal class TaskQueue
    {
        /// <summary>
        /// Sentinel has a blocking role so that at a time only one immutable operation can be performed.
        /// </summary>
        private static readonly object Sentinel = new object();
        /// <summary>
        /// Returns the result when an asynchronous operation completes.
        /// </summary>
        private Task prev = Task.FromResult(Sentinel);
        /// <summary>
        /// Add asynchronous operations to TaskQueue.
        /// </summary>
        /// <param name="action">An asynchronous operation.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task Enqueue(Func<Task> action)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            await Interlocked.Exchange(ref prev, tcs.Task).ConfigureAwait(false);
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                tcs.SetResult(Sentinel);
            }
        }
        /// <summary>
        /// Add asynchronous operations to TaskQueue.
        /// </summary>
        /// <typeparam name="T">The data type of value.</typeparam>
        /// <param name="action">An asynchronous operation.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// Returned <typeparamref name="T"/> value.</returns>
        public async Task<T> Enqueue<T>(Func<Task<T>> action)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            await Interlocked.Exchange(ref prev, tcs.Task).ConfigureAwait(false);
            T obj;
            try
            {
                obj = await action().ConfigureAwait(false);
            }
            finally
            {
                tcs.SetResult(Sentinel);
            }
            return obj;
        }
       
    }
}
