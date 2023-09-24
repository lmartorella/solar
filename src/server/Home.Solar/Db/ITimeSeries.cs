using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lucky.Db
{
    /// <summary>
    /// Csv basic type of a time-stamp based record
    /// </summary>
    public class TimeSample
    {
        internal TimeSpan DaylightDelta = TimeSpan.Zero;

        /// <summary>
        /// Convert an invariant time to local time (DST/non-DST)
        /// </summary>
        public TimeSpan FromInvariantTime(TimeSpan ts)
        {
            return ts + DaylightDelta;
        }

        /// <summary>
        /// Convert an invariant time to local time (DST/non-DST)
        /// </summary>
        public DateTime FromInvariantTime(DateTime dt)
        {
            return dt + DaylightDelta;
        }

        [Csv("HH:mm:ss")]
        public DateTime TimeStamp;
    }

    /// <summary>
    /// Csv for a day of samples, that supports custom aggregation
    /// </summary>
    public abstract class DayTimeSample<T> where T : TimeSample
    {
        internal TimeSpan DaylightDelta = TimeSpan.Zero;

        /// <summary>
        /// Convert an invariant time to local time (DST/non-DST)
        /// </summary>
        public TimeSpan FromInvariantTime(TimeSpan ts)
        {
            return ts + DaylightDelta;
        }

        [Csv("yyyy-MM-dd")]
        public DateTime Date;

        /// <summary>
        /// Aggregate one day of samples in a single sample line of a different time series
        /// </summary>
        public abstract bool Aggregate(DateTime date, IEnumerable<T> samples);
    }

    /// <summary>
    /// A time series that support storage day-by-day
    /// </summary>
    public interface ITimeSeries
    {
        /// <summary>
        /// Called at startup to sync aggregate data
        /// </summary>
        Task Init(DateTime now);

        /// <summary>
        /// Called at the end of a day to open a new csv file
        /// </summary>
        Task Rotate(DateTime start);
    }

    /// <summary>
    /// Typed time series
    /// </summary>
    public interface ITimeSeries<T, Taggr> : ITimeSeries where T : TimeSample where Taggr : DayTimeSample<T>
    {
        /// <summary>
        /// Register new sample
        /// </summary>
        void AddNewSample(T sample);

        /// <summary>
        /// Retrieve the last sample, if one
        /// </summary>
        T GetLastSample();

        /// <summary>
        /// Get the current period data aggregated with the Taggr logic.
        /// </summary>
        Taggr GetAggregatedData();
    }
}
