using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace Lucky.Home.Model
{
    public class TimeProgram<TCycle> where TCycle : TimeProgram<TCycle>.Cycle
    {
        // Cover the timelapse during DST
        private static TimeSpan RefreshTimerPeriod = TimeSpan.FromHours(6);

        private ProgramData _program;
        private Timer _refreshTimer;
        private Timer[] _intermediateTimers = new Timer[0];
        private ILogger _logger;
        // Make sure that we don't lose ticks in between poll calc
        private DateTime _lastRefreshTime;

        public static ProgramData DefaultProgram
        {
            get
            {
                return new ProgramData { Cycles = new TCycle[0] };
            }
        }

        public TimeProgram(ILogger logger)
        {
            _logger = logger;
            SetProgram(DefaultProgram);
        }

        public void Dispose()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }
        }

        public ProgramData Program
        {
            get
            {
                return _program;
            }
        }

        public void SetProgram(ProgramData value)
        {
            Validate(value);
            _program = value;
            InitTimers();
        }

        private static void Validate(ProgramData program)
        {
            if (program == null || program.Cycles == null)
            {
                throw new ArgumentNullException("program", "Missing program");
            }
            foreach (var t in program.Cycles.Select((cycle, idx) => new { cycle, idx }))
            {
                var name = t.cycle.Name ?? t.idx.ToString();
                if (t.cycle.End.HasValue && t.cycle.Start.HasValue && t.cycle.Start > t.cycle.End)
                {
                    throw new ArgumentOutOfRangeException("cycle " + name, "End before start");
                }
                if (t.cycle.WeekDays != null && t.cycle.WeekDays.Length == 0)
                {
                    t.cycle.WeekDays = null;
                }
                if (t.cycle.DayPeriod > 0 && t.cycle.WeekDays != null)
                {
                    throw new ArgumentOutOfRangeException("cycle " + name, "Both day period and week days specified");
                }
                if (t.cycle.DayPeriod <= 0 && t.cycle.WeekDays == null)
                {
                    throw new ArgumentOutOfRangeException("cycle " + name, "No day period nor week days specified");
                }
                if (t.cycle.DayPeriod > 0 && (!t.cycle.Start.HasValue && !program.Start.HasValue))
                {
                    throw new ArgumentOutOfRangeException("cycle " + name, "No start date for periodic table, and no global start date");
                }
                if (t.cycle.StartTime < TimeSpan.Zero || t.cycle.StartTime > TimeSpan.FromDays(1))
                {
                    throw new ArgumentOutOfRangeException("cycle " + name, "Invalid start time");
                }
            }
        }

        /// <summary>
        /// The program data
        /// </summary>
        [DataContract]
        public class ProgramData
        {
            /// <summary>
            /// List of programs (if requested)
            /// </summary>
            [DataMember(Name = "cycles")]
            public TCycle[] Cycles { get; set; }

            /// <summary>
            /// Global start date (e.g. for rain-disabling)?
            /// </summary>
            [DataMember(Name = "start")]
            public string StartStr { get { return Start.ToIso(); } set { Start = value.FromIso(); } }
            [IgnoreDataMember]
            public DateTime? Start { get; set; }
        }

        /// <summary>
        /// One cycle program
        /// </summary>
        [DataContract]
        public class Cycle
        {
            /// <summary>
            /// Friendly name
            /// </summary>
            [DataMember(Name = "name")]
            public string Name { get; set; }

            /// <summary>
            /// Enable/Disabled
            /// </summary>
            [DataMember(Name = "disabled")]
            public bool Disabled { get; set; }

            /// <summary>
            /// Start date-time
            /// </summary>
            [DataMember(Name = "start")]
            public string StartStr { get { return Start.ToIso();  } set { Start = value.FromIso();  } }

            /// <summary>
            /// Start date-time
            /// </summary>
            [IgnoreDataMember]
            public DateTime? Start { get; set; }

            /// <summary>
            /// End date-time
            /// </summary>
            [DataMember(Name = "end")]
            public string EndStr { get { return End.ToIso(); } set { End = value.FromIso(); } }

            /// <summary>
            /// End date-time
            /// </summary>
            [IgnoreDataMember]
            public DateTime? End { get; set; }

            /// <summary>
            /// If > 0, period in number of days
            /// </summary>
            [DataMember(Name = "dayPeriod")]
            public int DayPeriod { get; set; }

            /// <summary>
            /// If not null, week days of activity (0 = Sunday, 6 = Saturday) 
            /// </summary>
            [DataMember(Name = "weekDays")]
            public DayOfWeek[] WeekDays { get; set; }

            /// <summary>
            /// Time of day of start activity
            /// </summary>
            [DataMember(Name = "startTime")]
            public string StartTimeStr { get { return ToIso(StartTime); } set { StartTime = FromIsoT(value); } }

            /// <summary>
            /// Time of day of start activity
            /// </summary>
            [IgnoreDataMember]
            public TimeSpan StartTime { get; set; }
        }

        private static string ToIso(TimeSpan? timeOfDay)
        {
            if (timeOfDay.HasValue)
            {
                return timeOfDay.Value.ToString("c");
            }
            else
            {
                return null;
            }
        }

        private static TimeSpan FromIsoT(string str)
        {
            return TimeSpan.ParseExact(str, "c", null);
        }

        public class CycleTriggeredEventArgs : EventArgs
        {
            public TCycle Cycle;
        }

        /// <summary>
        /// Event raised when a cycle program kicks in
        /// </summary>
        public event EventHandler<CycleTriggeredEventArgs> CycleTriggered;

        private void InitTimers()
        {
            // Building a straightforward C# timer for each cycle pointing to the next step is limited to 49 days.
            // This will require an additional refresh timer for longer periods.
            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
            }
            foreach (var timer in _intermediateTimers)
            {
                timer?.Dispose();
            }
            _intermediateTimers = new Timer[0];

            _lastRefreshTime = DateTime.Now;
            CheckRefresh();
        }

        private void CheckRefresh()
        {
            //_logger.Log("RefreshTimer");
            var nextTimestamp = _lastRefreshTime + RefreshTimerPeriod;
            _intermediateTimers = CreateIntermediateTimers(_lastRefreshTime, nextTimestamp);
            _lastRefreshTime = nextTimestamp;

            // To avoid GC
            _refreshTimer = new Timer(o => CheckRefresh(), null, (_lastRefreshTime - DateTime.Now), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Create shorter C# timers for each cycle that triggers between now and pollPeriod
        /// </summary>
        private Timer[] CreateIntermediateTimers(DateTime begin, DateTime end)
        {
            return GetNextCycles(begin).TakeWhile(c => c.Item2 < end).Select(tuple =>
            {
                // Ok, schedule the timer for this period
                return new Timer(o => RaiseEvent(tuple.Item1), null, tuple.Item2 - DateTime.Now, Timeout.InfiniteTimeSpan);
            }).ToArray();
        }

        public IEnumerable<Tuple<TCycle, DateTime>> GetNextCycles(DateTime now)
        {
            if (_program != null)
            {
                return GetNextCycles(_program, now);
            }
            else
            {
                return new Tuple<TCycle, DateTime>[0];
            }
        }

        public static IEnumerable<Tuple<TCycle, DateTime>> GetNextCycles(ProgramData program, DateTime now)
        {
            if (program.Cycles == null || program.Cycles.Length == 0)
            {
                yield break;
            }

            // Next times
            Tuple<DateTime, int>[] nextTimes = program.Cycles.Select((cycle, i) => Tuple.Create(GetNextTick(program, cycle, now), i)).ToArray();

            // Take the closer one
            while (true)
            {
                Tuple<DateTime, int> closer = nextTimes.OrderBy(tuple => tuple.Item1).First();

                if (closer.Item1 == DateTime.MaxValue)
                {
                    yield break;
                }
                var index = closer.Item2;
                var cycle = program.Cycles[index];
                var nextTime = closer.Item1;
                yield return Tuple.Create(cycle, nextTime);

                // Replace with next
                nextTimes[index] = Tuple.Create(GetNextTick(program, cycle, nextTime + TimeSpan.FromSeconds(1)), index);
            }
        }

        private void RaiseEvent(TCycle cycle)
        {
            CycleTriggered?.Invoke(this, new CycleTriggeredEventArgs { Cycle = cycle });
        }

        /// <summary>
        /// Get the next event timestamp for that cycle starting from now
        /// </summary>
        public static DateTime GetNextTick(ProgramData program, Cycle cycle, DateTime now)
        {
            if (cycle.Disabled)
            {
                return DateTime.MaxValue;
            }

            // Check begin / end
            if (cycle.Start.HasValue && now < cycle.Start.Value)
            {
                now = cycle.Start.Value;
            }
            if (program.Start.HasValue && now < program.Start.Value)
            {
                now = program.Start.Value;
            }
            if (cycle.End.HasValue && now > cycle.End)
            {
                return DateTime.MaxValue;
            }

            // Calc the next valid starting day 
            var nextDay = (now.TimeOfDay >= cycle.StartTime) ? (now.Date + TimeSpan.FromDays(1)) : now.Date;
            if (cycle.WeekDays != null)
            {
                // Get the next weekday
                nextDay = GetNextValidWeekday(cycle.WeekDays, nextDay);
            }
            else
            {
                // Get the next periodic day
                nextDay = GetNextValidPeriodicDay(cycle.DayPeriod, nextDay, cycle.Start.HasValue ? cycle.Start.Value : program.Start.Value);
            }

            return nextDay + cycle.StartTime;
        }

        private static DateTime GetNextValidPeriodicDay(int dayPeriod, DateTime today, DateTime startDate)
        {
            int elapsedDays = (int)Math.Round((today.Subtract(startDate)).TotalDays);
            // If elapsedDays is multiple of dayPeriod, don't add any day
            // If elapsedDays is (multiple of dayPeriod + 1), add (dayPeriod - 1), etc.. 
            int missingDays = (dayPeriod - (elapsedDays % dayPeriod)) % dayPeriod;
            return today + TimeSpan.FromDays(missingDays);
        }

        private static DateTime GetNextValidWeekday(DayOfWeek[] weekDays, DateTime today)
        {
            for (var i = 0; i < 7; i++)
            {
                if (weekDays.Contains(today.DayOfWeek))
                {
                    break;
                }
                today += TimeSpan.FromDays(1);
            }
            return today;
        }
    }
}
