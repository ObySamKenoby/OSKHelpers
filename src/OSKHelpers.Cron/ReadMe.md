# OSKHelpers.Cron

**OSKHelpers.Cron** schedules the actions of an application from a `crontab` file, with the standard cron syntax interpreted
by [Cronos](https://github.com/HangfireIO/Cronos). The scheduler reloads the file when it is modified, without a restart, and
raises an event (or awaits an asynchronous handler) at every scheduled time: the application decides what to execute.
It targets **.NET Standard 2.0** and integrates with [OSKHelpers](https://www.nuget.org/packages/OSKHelpers) (logging via
`SimpleLog`, default directory via `Paths`, settings via `JsonSettings<T>`).

```
dotnet add package OSKHelpers.Cron
```

Source code, issue tracking and the other packages:
[github.com/ObySamKenoby/OSKHelpers](https://github.com/ObySamKenoby/OSKHelpers).

> **Compatibility**: OSKHelpers.Cron 0.9.0 requires OSKHelpers **2.0.0** or later; the automatic update of the settings
> shown in [Settings](#settings-isettingscron) requires OSKHelpers **2.1.3**.

---

## The `crontab` file

By default the file is `crontab` (no extension) in `Paths.DefaultConfigsDirectory`. Every line schedules an action:

```
<minute> <hour> <day of month> <month> <day of week> <Action>
<@macro> <Action>
```

```
# Every 15 minutes from 8 to 18, Monday to Friday
*/15 8-18 * * 1-5   Import orders
# Every day at midnight
@daily              Cleanup
# The same action can appear in more lines
30 12 * * 6         Import orders
```

- The action is the rest of the line and may contain spaces; by default actions are compared without distinction between upper
  and lower case (see [Comparing the actions](#comparing-the-actions)).
- Macros: `@yearly`/`@annually`, `@monthly`, `@weekly`, `@daily`/`@midnight`, `@hourly`, `@every_minute`.
- Empty lines and lines starting with `#` are ignored; invalid lines are ignored by the scheduler and reported.
- The times are interpreted in the local time zone (or in the one configured), including the daylight saving time changes.

---

## Quick start

```csharp
using OSKHelpers.Cron;

var scheduler = new CrontabScheduler(new Crontab());

// Events and handlers must be set before Start, which reads the file immediately
scheduler.CrontabReloaded += (sender, e) =>
{
    if (e.Errors.Count > 0) { /* e.g. send a warning */ }
};
scheduler.ActionHandler = e => RunActionAsync(e.Action, e.CancellationToken);

scheduler.Start();
// ...
scheduler.Stop();       // waits for the running actions up to StopTimeoutSeconds (default 5 minutes)
```

### Event or `ActionHandler`

- `ScheduledActionTriggered` is raised in a task of the thread pool: its handlers must be **synchronous**.
- `ActionHandler` (`Func<ScheduledActionTriggeredEventArgs, Task>`) is awaited by the scheduler: use it for asynchronous code.

Do not write an `async` handler for the event: it returns at its first `await`, so the scheduler considers the action
terminated while it is still running (overlapping executions are not prevented, `Stop` does not wait for it) and an exception
thrown after the `await` can terminate the process. If you prefer the event, wait for the asynchronous code in it:

```csharp
scheduler.ScheduledActionTriggered += (sender, e) =>
{
    RunActionAsync(e.Action, e.CancellationToken).GetAwaiter().GetResult();
};
```

### Behaviour

- At every wake-up the scheduler executes the actions with a scheduled time since the previous wake-up: scheduled times before
  the start are not recovered; the times missed by a late wake-up (e.g. after the system was suspended) produce a single
  execution (`MissedOccurrences`).
- An action scheduled by more lines at the same time is executed once.
- If an action is still running at its next scheduled time, that time is skipped (`ScheduledActionSkipped`).
- Two lines schedule the same action when `Crontab.ActionComparer` considers their actions equal (see
  [Comparing the actions](#comparing-the-actions)).
- `NextOccurrenceUtc` and `NextOccurrenceActions` show the next scheduled time and its actions.
- Do not call `Stop` from the handlers of the scheduler.
- `Dispose` stops the scheduler only if `Stop`/`StopAsync` has not been called after the last `Start`: the actions still
  running after the stop timeout are not awaited again, so a hosted service can stop the scheduler in `ExecuteAsync` and
  dispose it with the service.

### Use in a hosted service

```csharp
public class Worker : BackgroundService
{
    private readonly CrontabScheduler _scheduler;

    public Worker()
    {
        _scheduler = new CrontabScheduler(new AppCrontab());
        _scheduler.ActionHandler = e => RunActionAsync(e.Action, e.CancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _scheduler.Start();
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        await _scheduler.StopAsync();
    }
}
```

---

## Checking the file

`CheckCrontab` reads the file and returns its validity and descriptive lines (errors prefixed by `ERROR: `, warnings by
`WARNING: `), including the coverage of every action: number of lines, next execution, executions in the next 24 hours and
7 days, least frequent actions.

```csharp
var check = new Crontab().CheckCrontab();
check.Lines.ForEach(Console.WriteLine);
return check.IsValid ? 0 : 1;
```

### Custom validation

Derive from `Crontab` to add the rules of the application:

- `ValidateAction` rejects a line (used also by the scheduler, which ignores it): e.g. an unknown action;
- `AddCoverageLines` writes the coverage table: override it to show a table of your own (e.g. all the configured actions
  with their state, using `GetCoverage` with the list of the actions and `GetLeastCovered`) or to omit it;
- `CheckCrontabCustomLogic` adds lines to the check and can make the file invalid: e.g. configured actions not scheduled.

```csharp
public class AppCrontab : Crontab
{
    protected override string ValidateAction(CrontabEntry entry)
        => KnownActions.Contains(entry.Action)
            ? string.Empty
            : $"Line {entry.LineNumber}: the action '{entry.Action}' does not exist.";

    protected override bool CheckCrontabCustomLogic(IReadOnlyList<CrontabEntry> entries, List<string> lines)
    {
        var notScheduled = GetCoverage(entries, DateTime.UtcNow, Zone, KnownActions, ActionComparer)
                             .Where(c => !c.IsScheduled)
                             .Select(c => c.Action)
                             .ToList();
        if (notScheduled.Count > 0)
        {
            lines.Add($"{WARNINGPREFIX}actions not scheduled: {string.Join(", ", notScheduled)}.");
        }
        return true;    // warnings do not make the file invalid
    }

    protected override void AddCoverageLines(IReadOnlyList<CrontabEntry> entries, DateTime nowUtc, TimeZoneInfo zone,
        List<string> lines)
    {
        foreach (var item in GetCoverage(entries, nowUtc, zone, KnownActions, ActionComparer))
        {
            lines.Add($"  {item.Action,-30} {item.OccurrencesNextDay,8}{(item.IsScheduled ? "" : " [NOT SCHEDULED]")}");
        }
    }
}
```

When the list of the valid actions changes, call `Invalidate()` to reload the file at the next wake-up.

### Comparing the actions

By default two actions are the same when their texts are equal without distinction between upper and lower case
(`Crontab.DefaultActionComparer`). Pass a custom `IEqualityComparer<string>` to the constructor of `Crontab` to recognize as the
same action texts written in different ways, e.g. with different spaces or quotes: the scheduler then executes them once per
wake-up and does not start one of them while another is still running, minimizing the overlapping executions. The comparer is
also used by the coverage of `CheckCrontab`; an action scheduled by more lines is reported with the text of its first line.

```csharp
public class AppCrontab : Crontab
{
    public AppCrontab(string filePath) : base(filePath, null, new CommandComparer())
    {
    }
}
```

The comparer is set once, in the constructor (`ActionComparer`); its `GetHashCode` must be consistent with `Equals` and it must
be thread-safe.

---

## Settings (`ISettingsCron`)

Implement `ISettingsCron` in the settings class of the application and configure the scheduler with `Setup`; with
`[AutoUpdate]` the changes to the settings file are applied without a restart.

| Property                   | Meaning                                                                    | Recommended default                       |
| ---                        | ---                                                                        | ---                                       |
| `CronFilePath`             | Path of the file (relative to `Paths.DefaultConfigsDirectory`); empty = default | empty                                |
| `CronCheckIntervalSeconds` | Maximum wait between two checks of the file                                | `CrontabScheduler.MAXWAITSECONDS` (60)    |
| `CronStopTimeoutSeconds`   | Maximum wait for the running actions when stopped; `null` = no limit       | `CrontabScheduler.DEFAULTSTOPTIMEOUTSECONDS` (300) |
| `CronTimeZone`             | Id of the time zone; empty = local                                         | empty                                     |

The time zone ids differ between Windows (`W. Europe Standard Time`) and Linux (`Europe/Rome`) and are not converted
automatically on .NET Standard 2.0.

```csharp
public class AppSettings : JsonSettings<AppSettings>, ISettingsCron
{
    public static AppSettings Current { get; private set; }

    [AutoUpdate] public string CronFilePath { get; set; }
    [AutoUpdate] public int CronCheckIntervalSeconds { get; set; }
    [AutoUpdate] public int? CronStopTimeoutSeconds { get; set; }
    [AutoUpdate] public string CronTimeZone { get; set; }

    protected override void CreateDefaultData()
    {
        CronFilePath                = string.Empty;
        CronCheckIntervalSeconds    = CrontabScheduler.MAXWAITSECONDS;
        CronStopTimeoutSeconds      = CrontabScheduler.DEFAULTSTOPTIMEOUTSECONDS;
        CronTimeZone                = string.Empty;
    }
    // ...
}

AppSettings.Load();
var scheduler = new CrontabScheduler(new AppCrontab(), AppSettings.Current);      // same as Setup(AppSettings.Current)
AppSettings.Current.AutoUpdated += (sender, result) => scheduler.Setup(AppSettings.Current);
AppSettings.Current.StartAutoUpdate();
scheduler.Start();
```

`[AutoUpdate]`, `AutoUpdated` and `StartAutoUpdate` require OSKHelpers **2.1.3**; with earlier versions call `Setup` once at startup.

`Setup` checks all the values before applying any of them: with an invalid value nothing is applied, the reasons are written to
the log and the result is `false`.

---

## Release notes

- **0.9.0** - First release. The coverage table of the check can be replaced by overriding `AddCoverageLines`; `Dispose` does
  not stop again a scheduler already stopped; the actions are compared with `Crontab.ActionComparer`, which can be passed to
  the constructor of `Crontab`.
