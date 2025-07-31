// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Codex.Sdk.Search;

namespace Codex.Sdk
{
    /// <summary>
    /// Helpers for objects which track time. Replaces the need to use a new <see cref="Stopwatch"/> or <see cref="DateTime"/> for
    /// elapsed time measurements
    /// </summary>
    public static class TimestampUtilities
    {
        /// <summary>
        /// Shared stopwatch instance for operations which need to record elapsed time
        /// </summary>
        private static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();

        /// <summary>
        /// The current timestamp as a timespan
        /// </summary>
        public static TimeSpan Timestamp => s_stopwatch.Elapsed;

        public static void AtomicAdd(this ref TimeSpan t, TimeSpan value)
        {
            Interlocked.Add(ref t.TicksRef(), value.Ticks);
        }

        public static void Reset(this ref Timestamp t) => t = Sdk.Timestamp.New();

        public static Timestamp GetAndReset(this ref Timestamp t)
        {
            var snapshot = t;
            t.Reset();
            return snapshot;
        }

        public static ref long TicksRef(this ref TimeSpan t) => ref Unsafe.As<TimeSpan, long>(ref t);
    }

    public struct Timestamp(TimeSpan Start)
    {
        public static readonly Timestamp Instance = Timestamp.New();

        public static Timestamp New() => new(TimestampUtilities.Timestamp);

        public TimeSpan Elapsed => TimestampUtilities.Timestamp - Start;

        public override string ToString()
        {
            return Elapsed.ToString();
        }

        public static implicit operator TimeSpan(Timestamp t) => t.Elapsed;

        public static implicit operator SerializableTimeSpan(Timestamp t) => t.Elapsed;
    }
}