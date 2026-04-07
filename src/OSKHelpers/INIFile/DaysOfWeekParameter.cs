using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSKHelpers.INIFile
{
    public class DaysOfWeekParameter
    {
        #region Members

        private List<DayOfWeek> _daysOfWeek;

        #endregion

        #region Properties

        public IReadOnlyList<DayOfWeek> DaysOfWeek => _daysOfWeek;

        #endregion

        #region Constructors

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

        public void Add(int day)
        {
            if (!Enum.IsDefined(typeof(DayOfWeek), day))
            {
                throw new ArgumentNullException($"{SimpleLog.GetCallerTypeMethodName()}: the value {day} is not valid.");
            }
            Add((DayOfWeek)day);
        }

        public void Add(DayOfWeek day)
        {
            if (!_daysOfWeek.Contains(day))
            {
                _daysOfWeek.Add(day);
            }
        }

        public void Remove(int day)
        {
            if (!Enum.IsDefined(typeof(DayOfWeek), day))
            {
                throw new ArgumentNullException($"{SimpleLog.GetCallerTypeMethodName()}: the value {day} is not valid.");
            }
            Remove((DayOfWeek)day);
        }

        public void Remove(DayOfWeek day)
        {
            if (_daysOfWeek.Contains(day))
            {
                _daysOfWeek.Remove(day);
            }
        }

        public bool Contains(int day) => Enum.IsDefined(typeof(DayOfWeek), day) && _daysOfWeek.Contains((DayOfWeek)day);
        public bool Contains(DayOfWeek day) => _daysOfWeek.Contains(day);
        public bool ContainsToday() => Contains(DateTime.Now.DayOfWeek);

        public override string ToString() => string.Join(" ", _daysOfWeek.OrderBy(d => (int)d).Select(d => (int)d));

        #endregion
    }
}
