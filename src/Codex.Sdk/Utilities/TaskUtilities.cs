// --------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// --------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using BuildXL.Utilities.Threading;
using DotNext.Threading.Tasks;

namespace Codex.Utilities
{
    /// <summary>
    /// Static utilities related to <see cref="Task" />.
    /// </summary>
    public static partial class TaskUtilities
    {
        public static Task<bool> FalseTask { get; } = Task.FromResult(false);
        public static Task<bool> TrueTask { get; } = Task.FromResult(true);

        public static T LockedRead<T>(this in ReadWriteLock rwlock, ref T value)
        {
            using var _ = rwlock.AcquireReadLock();
            return value;
        }

        public static ValueTask CreateTask(this ValueTaskCompletionSource tcs)
        {
            return tcs.CreateTask(Timeout.InfiniteTimeSpan, default);
        }

        public static ValueTaskAwaiter GetAwaiter(this ValueTask? t)
        {
            return (t ?? ValueTask.CompletedTask).GetAwaiter();
        }

        public static Task AsValid(this Task? t)
        {
            return t ?? Task.CompletedTask;
        }

        public static ValueTask<T> CreateTask<T>(this ValueTaskCompletionSource<T> tcs)
        {
            return tcs.CreateTask(Timeout.InfiniteTimeSpan, default);
        }

        /// <summary>
        /// This is a variant of Task.WhenAll which ensures that all exceptions thrown by the tasks are
        /// propagated back through a single <see cref="AggregateException"/>. This is necessary because
        /// the default awaiter (as used by 'await') only takes the *first* exception inside of a task's
        /// aggregate exception. All BuildXL code should use this method instead of the standard WhenAll.
        /// </summary>
        /// <exception cref="System.AggregateException">Thrown when any of the tasks failed.</exception>
        public static async Task SafeWhenAll(IEnumerable<Task> tasks)
        {
            Contract.Requires(tasks != null);

            var whenAllTask = Task.WhenAll(tasks);

            try
            {
                await whenAllTask;
            }
            catch
            {
                if (whenAllTask.Exception != null)
                {
                    // Rethrowing the error preserving the stack trace.
                    ExceptionDispatchInfo.Capture(whenAllTask.Exception).Throw();
                }

                // whenAllTask is in the canceled state, we caught TaskCancelledException
                throw;
            }
        }

        public static Task<TResult[]> SelectAsync<TSource, TResult>(this ParallelOptions options, IReadOnlyCollection<TSource> items, Func<TSource, int, ValueTask<TResult>> body)
        {
            return SelectAsync(parallel: new(options), items, body);
        }

        public static async Task<TResult[]> SelectAsync<TSource, TResult>(Parallelism parallel, IReadOnlyCollection<TSource> items, Func<TSource, int, ValueTask<TResult>> body)
        {
            var result = new TResult[items.Count];

            await TaskUtilities.ForEachAsync(parallel, items.WithIndices(), async (t, token) =>
            {
                (var item, int index) = t;

                var itemResult = await body(item, index);

                result[index] = itemResult;
            });

            return result;
        }

        public record struct Parallelism(ParallelOptions? Options)
        {
            public static implicit operator Parallelism(bool value)
            {
                return new(value ? new ParallelOptions() : null);
            }

            public static implicit operator Parallelism(int parallelism)
            {
                return new Parallelism(parallelism > 1 ? new ParallelOptions()
                {
                    MaxDegreeOfParallelism = parallelism
                } : null);
            }
        }

        public static async Task ForEachAsync<TSource>(Parallelism parallel, IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body)
        {
            if (parallel.Options is { } options)
            {
                await Parallel.ForEachAsync(source, options, body);
            }
            else
            {
                foreach (var item in source)
                {
                    await body(item, default);
                }
            }
        }

        public static async Task ForEachAsync<TSource>(ParallelOptions parallelOptions, IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body)
        {
            await Parallel.ForEachAsync(source, parallelOptions, body);
        }

        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body)
        {
            return Parallel.ForEachAsync(source, body);
        }

        /// <summary>
        /// This is a variant of Task.WhenAll which ensures that all exceptions thrown by the tasks are
        /// propagated back through a single <see cref="AggregateException"/>. This is necessary because
        /// the default awaiter (as used by 'await') only takes the *first* exception inside of a task's
        /// aggregate exception. All BuildXL code should use this method instead of the standard WhenAll.
        /// </summary>
        /// <exception cref="System.AggregateException">Thrown when any of the tasks failed.</exception>
        public static async Task<TResult[]> SafeWhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            Contract.RequiresNotNull(tasks);

            var whenAllTask = Task.WhenAll(tasks);
            try
            {
                return await whenAllTask;
            }
            catch
            {
                if (whenAllTask.Exception != null)
                {
                    // Rethrowing the error preserving the stack trace.
                    ExceptionDispatchInfo.Capture(whenAllTask.Exception).Throw();
                }

                // whenAllTask is in the canceled state, we caught TaskCancelledException
                throw;
            }
        }

        /// <summary>
        /// This is a variant of Task.WhenAll which ensures that all exceptions thrown by the tasks are
        /// propagated back through a single <see cref="AggregateException"/>. This is necessary because
        /// the default awaiter (as used by 'await') only takes the *first* exception inside of a task's
        /// aggregate exception. All BuildXL code should use this method instead of the standard WhenAll.
        /// </summary>
        /// <exception cref="System.AggregateException">Thrown when any of the tasks failed.</exception>
        public static Task<TResult[]> SafeWhenAll<TResult>(params Task<TResult>[] tasks)
        {
            Contract.Requires(tasks != null);

            return SafeWhenAll((IEnumerable<Task<TResult>>)tasks);
        }


        /// <summary>
        /// This is a variant of Task.WhenAll which ensures that all exceptions thrown by the tasks are
        /// propagated back through a single <see cref="AggregateException"/>. This is necessary because
        /// the default awaiter (as used by 'await') only takes the *first* exception inside of a task's
        /// aggregate exception. All BuildXL code should use this method instead of the standard WhenAll.
        /// </summary>
        /// <exception cref="System.AggregateException">Thrown when any of the tasks failed.</exception>
        public static Task SafeWhenAll(params Task[] tasks)
        {
            Contract.Requires(tasks != null);

            return SafeWhenAll((IEnumerable<Task>)tasks);
        }

        public static void IgnoreAsync(this Task t)
        {
        }

        public static void IgnoreAsync(this ValueTask t)
        {
        }

        public static void IgnoreAsync<T>(this ValueTask<T> t)
        {
        }

        public static async ValueTask IgnoreResultAsync<T>(this ValueTask<T> t)
        {
            await t;
        }

        public static T GetTaskResult<T>(this ValueTask<T> t) => t.AsTask().GetAwaiter().GetResult();
        public static void GetTaskResult(this ValueTask t) => t.AsTask().GetAwaiter().GetResult();

        /// <summary>
        /// Returns a faulted task containing the given exception.
        /// This is the failure complement of <see cref="Task.FromResult{TResult}" />.
        /// </summary>
        [ContractOption("runtime", "checking", false)]
        public static Task<T> FromException<T>(Exception ex)
        {
            Contract.Requires(ex != null);
            Contract.Ensures(Contract.Result<Task<T>>() != null);

            var failureSource = new TaskCompletionSource<T>();
            failureSource.SetException(ex);
            return failureSource.Task;
        }

        /// <summary>
        /// Provides await functionality for ordinary <see cref="WaitHandle"/>s.
        /// </summary>
        /// <param name="handle">The handle to wait on.</param>
        /// <returns>The awaiter.</returns>
        public static TaskAwaiter GetAwaiter(this WaitHandle handle)
        {
            Contract.Requires(handle != null);

            return handle.ToTask().GetAwaiter();
        }

        /// <summary>
        /// Provides await functionality for an array of ordinary <see cref="WaitHandle"/>s.
        /// </summary>
        /// <param name="handles">The handles to wait on.</param>
        /// <returns>The awaiter.</returns>
        public static TaskAwaiter<int> GetAwaiter(this WaitHandle[] handles)
        {
            Contract.Requires(handles != null);
            Contract.Requires(Contract.ForAll(handles, handle => handles != null));

            return handles.ToTask().GetAwaiter();
        }

        public static ValueTask<T> ToValueTask<T>(this Task<T> task)
        {
            return new(task);
        }

        public static ValueTask ToValueTask(this Task task)
        {
            return new(task);
        }

        /// <summary>
        /// Creates a TPL Task that is marked as completed when a <see cref="WaitHandle"/> is signaled.
        /// </summary>
        /// <param name="handle">The handle whose signal triggers the task to be completed.  Do not use a <see cref="Mutex"/> here.</param>
        /// <param name="timeout">The timeout (in milliseconds) after which the task will fault with a <see cref="TimeoutException"/> if the handle is not signaled by that time.</param>
        /// <returns>A Task that is completed after the handle is signaled.</returns>
        /// <remarks>
        /// There is a (brief) time delay between when the handle is signaled and when the task is marked as completed.
        /// </remarks>
        public static Task ToTask(this WaitHandle handle, int timeout = Timeout.Infinite)
        {
            Contract.Requires(handle != null);

            return ToTask(new WaitHandle[1] { handle }, timeout);
        }

        /// <summary>
        /// Creates a TPL Task that is marked as completed when any <see cref="WaitHandle"/> in the array is signaled.
        /// </summary>
        /// <param name="handles">The handles whose signals triggers the task to be completed.  Do not use a <see cref="Mutex"/> here.</param>
        /// <param name="timeout">The timeout (in milliseconds) after which the task will return a value of WaitTimeout.</param>
        /// <returns>A Task that is completed after any handle is signaled.</returns>
        /// <remarks>
        /// There is a (brief) time delay between when the handles are signaled and when the task is marked as completed.
        /// </remarks>
        public static Task<int> ToTask(this WaitHandle[] handles, int timeout = Timeout.Infinite)
        {
            Contract.Requires(handles != null);
            Contract.Requires(Contract.ForAll(handles, handle => handles != null));

            var tcs = new TaskCompletionSource<int>();
            int signalledHandle = WaitHandle.WaitAny(handles, 0);
            if (signalledHandle != WaitHandle.WaitTimeout)
            {
                // An optimization for if the handle is already signaled
                // to return a completed task.
                tcs.SetResult(signalledHandle);
            }
            else
            {
                var localVariableInitLock = new object();
                lock (localVariableInitLock)
                {
                    RegisteredWaitHandle[] callbackHandles = new RegisteredWaitHandle[handles.Length];
                    for (int i = 0; i < handles.Length; i++)
                    {
                        callbackHandles[i] = ThreadPool.RegisterWaitForSingleObject(
                            handles[i],
                            (state, timedOut) =>
                            {
                                int handleIndex = (int)state;
                                if (timedOut)
                                {
                                    tcs.TrySetResult(WaitHandle.WaitTimeout);
                                }
                                else
                                {
                                    tcs.TrySetResult(handleIndex);
                                }

                                // We take a lock here to make sure the outer method has completed setting the local variable callbackHandles contents.
                                lock (localVariableInitLock)
                                {
                                    foreach (var handle in callbackHandles)
                                    {
                                        handle.Unregister(null);
                                    }
                                }
                            },
                            state: i,
                            millisecondsTimeOutInterval: timeout,
                            executeOnlyOnce: true);
                    }
                }
            }

            return tcs.Task;
        }

        /// <summary>
        /// Creates a new <see cref="SemaphoreSlim"/> representing a mutex which can only be entered once.
        /// </summary>
        /// <returns>the semaphore</returns>
        public static SemaphoreSlim CreateMutex()
        {
            return new SemaphoreSlim(initialCount: 1, maxCount: 1);
        }

        /// <summary>
        /// Asynchronously acquire a semaphore
        /// </summary>
        /// <param name="semaphore">The semaphore to acquire</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>A disposable which will release the semaphore when it is disposed.</returns>
        public static async ValueTask<SemaphoreReleaser> AcquireAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(semaphore != null);
            await semaphore.WaitAsync(cancellationToken);
            return new SemaphoreReleaser(semaphore);
        }

        /// <summary>
        /// Synchronously acquire a semaphore
        /// </summary>
        /// <param name="semaphore">The semaphore to acquire</param>
        public static SemaphoreReleaser AcquireSemaphore(this SemaphoreSlim semaphore)
        {
            Contract.Requires(semaphore != null);
            semaphore.Wait();
            return new SemaphoreReleaser(semaphore);
        }

        public static SemaphoreReleaser TryAcquireSemaphore(this SemaphoreSlim semaphore,
            out bool acquired,
            TimeSpan timeout = default)
        {
            Contract.Requires(semaphore != null);
            acquired = semaphore.Wait(timeout);
            return new SemaphoreReleaser(acquired ? semaphore : null);
        }

        /// <summary>
        /// Consumes a task and doesn't do anything with it.  Useful for fire-and-forget calls to async methods within async methods.
        /// </summary>
        /// <param name="task">The task whose result is to be ignored.</param>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this Task task)
        {
        }

        public static async Task<TResult> ThenAsync<T, TResult>(this Task<T> task, Func<T, Task<TResult>> func)
        {
            var source = await task;
            return await func(source);
        }

        public static async Task<TResult> SelectAsync<T, TResult>(this Task<T> task, Func<T, TResult> func)
        {
            var source = await task;
            return func(source);
        }

        public static AsyncLocalScope<T> EnterScope<T>(this AsyncLocal<T> local, T value, T exitValue = default)
        {
            local.Value = value;
            return new(local, exitValue);
        }

        public static SynchronizationContextAwaitable SwitchTo(this SynchronizationContext context)
        {
            return new SynchronizationContextAwaitable(context);
        }

        public record struct SynchronizationContextAwaitable(SynchronizationContext Context) : INotifyCompletion, ICriticalNotifyCompletion
        {
            public SynchronizationContextAwaitable GetAwaiter()
            {
                return this;
            }

            public void GetResult()
            {
            }

            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                if (SynchronizationContext.Current == Context)
                {
                    continuation();
                }
                else
                {
                    Context.Post(s =>
                    {
                        continuation();
                    }, null);
                }
            }
        }

        /// <summary>
        /// Allows an IDisposable-conforming release of an acquired semaphore
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
        public struct SemaphoreReleaser : IDisposable
        {
            public bool IsAcquired => m_semaphore != null;

            private readonly SemaphoreSlim m_semaphore;

            /// <summary>
            /// Creates a new releaser.
            /// </summary>
            /// <param name="semaphore">The semaphore to release when Dispose is invoked.</param>
            /// <remarks>
            /// Assumes the semaphore is already acquired.
            /// </remarks>
            internal SemaphoreReleaser(SemaphoreSlim semaphore)
            {
                this.m_semaphore = semaphore;
            }

            /// <summary>
            /// IDispoaable.Dispose()
            /// </summary>
            public void Dispose()
            {
                m_semaphore?.Release();
            }

            /// <summary>
            /// Whether this semaphore releaser is valid (and not the default value)
            /// </summary>
            public bool IsValid
            {
                get { return m_semaphore != null; }
            }

            /// <summary>
            /// Gets the number of threads that will be allowed to enter the semaphore.
            /// </summary>
            public int CurrentCount
            {
                get { return m_semaphore.CurrentCount; }
            }
        }
    }
}