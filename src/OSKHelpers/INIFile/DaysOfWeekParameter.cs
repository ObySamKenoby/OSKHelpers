using OSKHelpers.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OSKHelpers.INIFile
{
    public class DaysOfWeekParameter
    {
        #region Membri

        private List<DayOfWeek> _daysOfWeek;

        #endregion

        #region Proprietà

        public IReadOnlyList<DayOfWeek> DaysOfWeek => _daysOfWeek;

        #endregion

        #region Costruttori

        public DaysOfWeekParameter() 
        { 
            _daysOfWeek = new List<DayOfWeek>();
        }

        /// <summary>
        /// Questo costruttore accetta un parametro che permette di passare i giorni della settimana.<br/>
        /// I giorni devono essere indicati all'interno di un'unica stringa contenente i valori Int dei giorni (0 Domenica - 6 Sabato) separati da spazi.<br/>
        /// Se il parametro passato è nullo o vuoto o i singoli valori non sono validi eleva la relativa eccezione.
        /// </summary>
        /// <param name="daysOfWeek">Stringa contenente i giorni rappresentati da interi separati da spazi.</param>
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
                    throw new ArgumentOutOfRangeException($"{SimpleLog.GetCallerTypeMethodName()}: l'elenco dei giorni contiene un valore non valido: {day}");
                }
                Add(d);
            }
        }

        #endregion

        #region Metodi

        public void Add(int day)
        {
            if (!Enum.IsDefined(typeof(DayOfWeek), day))
            {
                throw new ArgumentNullException($"{SimpleLog.GetCallerTypeMethodName()}: il valore {day} non è valido.");
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
                throw new ArgumentNullException($"{SimpleLog.GetCallerTypeMethodName()}: il valore {day} non è valido.");
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
