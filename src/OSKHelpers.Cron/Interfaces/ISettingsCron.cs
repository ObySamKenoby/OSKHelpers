namespace OSKHelpers.Cron.Interfaces
{
    /// <summary>
    /// Parameters of the <see cref="CrontabScheduler"/> read from a settings file, applied with
    /// <see cref="CrontabScheduler.Setup(ISettingsCron)"/>.<br/>
    /// Usually implemented by a class derived from <c>OSKHelpers.Json.JsonSettings&lt;T&gt;</c>: assign the recommended default
    /// values in <c>CreateDefaultData</c> and mark the properties with <c>[AutoUpdate]</c> to apply their changes without a
    /// restart (call <see cref="CrontabScheduler.Setup(ISettingsCron)"/> again in the handler of <c>AutoUpdated</c>).
    /// </summary>
    public interface ISettingsCron
    {
        /// <summary>
        /// Path of the <c>crontab</c> file; a relative path is resolved on <c>Paths.DefaultConfigsDirectory</c>, an empty
        /// value means <see cref="Crontab.DefaultFilePath"/>.<br/>
        /// Recommended default: empty.
        /// </summary>
        string CronFilePath { get; set; }

        /// <summary>
        /// Maximum wait, in seconds, between two wake-ups of the scheduler, i.e. the interval of the check of the changes to
        /// the file (<see cref="CrontabScheduler.MaxWaitSeconds"/>); at least 1.<br/>
        /// Recommended default: <see cref="CrontabScheduler.MAXWAITSECONDS"/>.
        /// </summary>
        int CronCheckIntervalSeconds { get; set; }

        /// <summary>
        /// Maximum wait, in seconds, for the running actions when the scheduler is stopped
        /// (<see cref="CrontabScheduler.StopTimeoutSeconds"/>); null means no limit, otherwise at least 0.<br/>
        /// Recommended default: <see cref="CrontabScheduler.DEFAULTSTOPTIMEOUTSECONDS"/>.
        /// </summary>
        int? CronStopTimeoutSeconds { get; set; }

        /// <summary>
        /// Id of the time zone in which the cron expressions are interpreted (<c>TimeZoneInfo.FindSystemTimeZoneById</c>); an
        /// empty value means the local time zone.<br/>
        /// <b>Note</b>: the ids differ between Windows (e.g. <c>W. Europe Standard Time</c>) and Linux (e.g.
        /// <c>Europe/Rome</c>), and on .NET Standard 2.0 they are not converted automatically.<br/>
        /// Recommended default: empty.
        /// </summary>
        string CronTimeZone { get; set; }
    }
}
