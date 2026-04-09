using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSKHelpers.INIFile
{
    /// <summary>
    /// Parses a space-separated list of integer day codes (0 = Sunday … 6 = Saturday)
    /// into a typed <see cref="IReadOnlyList{DayOfWeek}"/>.
    /// </summary>
    public class DaysOfWeekParameter
    {
        #region Members

        /// <summary>
        /// Internal list of days of the week.
        /// </summary>
        private List<DayOfWeek> _daysOfWeek;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the read-only list of days of the week.
        /// </summary>
        public IReadOnlyList<DayOfWeek> DaysOfWeek => _daysOfWeek;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new empty instance of <see cref="DaysOfWeekParameter"/>.
        /// </summary>
        public DaysOfWeekParameter() 
        { 
            _daysOfWeek = new List<DayOfWeek>();
        }

        /// <summary>
        /// Initialises a new instance accepting a string that specifies the days of the week.<br/>
        /// Days must be provided as a single string containing the integer values of the days (0 Sunday - 6 Saturday) separated by spaces.<br/>
        /// Throws an exception if the parameter is null, empty, or contains invalid values.
        /// </summary>
        /// <param name="daysOfWeek">String containing the days represented as space-separated integers.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DaysOfWeekParameter(string daysOfWeek) : this() 
        { 
            if (string.IsNullOrWhiteSpace(daysOfWeek))
            {
                throw new ArgumentNullException(nameof(daysOfWeek));
            }
            
            var days = daysOfWeek.Trim().Split(' ').Select(v => v.Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            foreach (var day in days)
            {
                if (!int.TryParse(day, out int d) || !Enum.IsDefined(typeof(DayOfWeek), d))
                {
                    throw new ArgumentOutOfRangeException($"{SimpleLog.GetCallerTypeMethodName()}: the days list contains an invalid value: {day}");
                }
                Add(d);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds the given day by its integer code.
        /// </summary>
        /// <param name="day">Integer code of the day (0 = Sunday … 6 = Saturday).</param>
        /// <exception cref="ArgumentNullException"/>
        public void Add(int day)
        {
            if (!Enum.IsDefined(typeof(DayOfWeek), day))
            {
                throw new ArgumentNullException($"{SimpleLog.GetCallerTypeMethodName()}: the value {day} is not valid.");
            }
            Add((DayOfWeek)day);
        }

        /// <summary>
        /// Adds the given <see cref="DayOfWeek"/> value if not already present.
        /// </summary>
        /// <param name="day">Day to add.</param>
        public void Add(DayOfWeek day)
        {
            if (!_daysOfWeek.Contains(day))
            {
                _daysOfWeek.Add(day);
            }
        }

        /// <summary>
        /// Removes the given day by its integer code.
        /// </summary>
        /// <param name="day">Integer code of the day (0 = Sunday … 6 = Saturday).</param>
        /// <exception cref="ArgumentNullException"/>
        public void Remove(int day)
        {
            if (!Enum.IsDefined(typeof(DayOfWeek), day))
            {
                throw new ArgumentNullException($"{SimpleLog.GetCallerTypeMethodName()}: the value {day} is not valid.");
            }
            Remove((DayOfWeek)day);
        }

        /// <summary>
        /// Removes the given <see cref="DayOfWeek"/> value if present.
        /// </summary>
        /// <param name="day">Day to remove.</param>
        public void Remove(DayOfWeek day)
        {
            if (_daysOfWeek.Contains(day))
            {
                _daysOfWeek.Remove(day);
            }
        }

        /// <summary>
        /// Returns true if the given day code is in the list.
        /// </summary>
        /// <param name="day">Integer code of the day.</param>
        public bool Contains(int day) => Enum.IsDefined(typeof(DayOfWeek), day) && _daysOfWeek.Contains((DayOfWeek)day);

        /// <summary>
        /// Returns true if the given <see cref="DayOfWeek"/> is in the list.
        /// </summary>
        /// <param name="day">Day to check.</param>
        public bool Contains(DayOfWeek day) => _daysOfWeek.Contains(day);

        /// <summary>
        /// Returns true if today's day of week is in the list.
        /// </summary>
        public bool ContainsToday() => Contains(DateTime.Now.DayOfWeek);

        /// <summary>
        /// Returns a space-separated string of integer day codes, ordered ascending.
        /// </summary>
        public override string ToString() => string.Join(" ", _daysOfWeek.OrderBy(d => (int)d).Select(d => (int)d));

        #endregion
    }
}
