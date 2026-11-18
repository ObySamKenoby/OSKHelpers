# OSKHelpers

**OSKHelpers** is a collection of general-purpose, independent utility classes targeting **.NET Standard 2.0** and **.NET 8**.
Each namespace addresses a specific concern and has no mandatory coupling with the others, so you can use only what you need:
logging, INI and JSON settings files, default paths, password hashing and credential encryption, collections, network and
generic helpers.

```
dotnet add package OSKHelpers
```

The e-mail helpers are in the separate [OSKHelpers.Mail](https://www.nuget.org/packages/OSKHelpers.Mail) package, the scheduling
of actions from a crontab file in [OSKHelpers.Cron](https://www.nuget.org/packages/OSKHelpers.Cron). Source code,
issue tracking and the other packages: [github.com/ObySamKenoby/OSKHelpers](https://github.com/ObySamKenoby/OSKHelpers).

---

## Quick start

### Logging

`SimpleLog` is a static façade over a `SimpleLogger` instance: no configuration is needed.

```csharp
using OSKHelpers.Logging;

SimpleLog.Write("Application started");                 // always written
SimpleLog.ErrorWrite("Something went wrong");           // written with LogLevel >= Error
SimpleLog.DebugWrite("Details", new { Id = 1 });        // written with LogLevel >= Debug, object serialised as JSON

try
{
    // ...
}
catch (Exception ex)
{
    SimpleLog.LogError(ex);                             // "Error in Class.Method: exception"
}
```

- One file per day, `<Prefix>Log<yyyyMMdd>.txt`, in `Paths.DefaultLogsDirectory` (`<application folder>/Logs`, or
  `/app/appdata/Logs` in a container), created at the first write.
- Each line starts with date and time, followed by `DEBUG` or `PROTOCOL` for those levels.
- The default level is `Error` (`Debug` when the library is built in Debug); it can be read from settings with
  `SimpleLog.SetLogLevel(iniFile)` or `SimpleLog.SetLogLevel(settings)`.
- For separate logs (e.g. a device) create a `SimpleLogger` with its own `Prefix` or `LogPath`.

#### Archiving of the old logs

```csharp
SimpleLog.ArchiveRetentionMonths    = 1;                // complete months kept uncompressed (default 1, minimum 1)
SimpleLog.EnableArchiving           = true;             // disabled by default
```

With archiving enabled, every time the log file name changes (first write, change of date, folder or prefix) a background
thread moves the daily files of the old months into a monthly zip file, `<Prefix>Log_<yyyy>_<MM>.zip`, in the same folder.
The current month and the previous `ArchiveRetentionMonths` months stay uncompressed: with the default value, during 09/2026 the
files up to 07/2026 are archived, so the log folder holds the logs of the last 30 - 60 days. Neither the log files nor the zip
files are ever deleted; an existing zip file is updated, and an entry with the name of an existing one gets a `_N` suffix.
`ArchiveLogs()` archives at once, also with archiving disabled.

### Settings in an INI file

```ini
# Settings.ini, in the application folder
LogLevel    = 4
ServerName  = srv01
Retries     = 3
*Recipients = first@example.com
*Recipients = second@example.com
```

```csharp
using OSKHelpers.INIFile;

var iniFile = new IniFileHelper(true);                  // reads Settings.ini
if (iniFile.IniFileRead)
{
    SimpleLog.SetLogLevel(iniFile);
    var server      = iniFile.GetString("ServerName");
    var retries     = iniFile.GetInt("Retries", 1);     // default value if missing or not valid
    var recipients  = iniFile.Array("Recipients");
}
```

Keys are case-insensitive, `#` starts a comment line, `*NAME` defines an array. The `!!DEBUG!!` and `!!PROTOCOL!!` pragmas on
the first lines raise the log level while the file is read. A commented template (`SettingsTemplate.ini`) is included in the
package. With the default path, `Settings.ini` is looked up in the application folder and, if missing there, in
`Paths.AppdataDirectory`, where `Paths.MigrateToAppdata()` moves it (see "Data in the Appdata directory").

### Settings in a JSON file

```csharp
using OSKHelpers.Json;

public class AppSettings : JsonSettings<AppSettings>
{
    protected override int LASTVERSION => 2;           // increase it when properties are added

    public string ServerName { get; set; }

    protected override void CreateDefaultData() => ServerName = "localhost";
    public override void Init() { }
    public override bool CheckIsValid() => IsValid = !string.IsNullOrWhiteSpace(ServerName);
}

var (settings, isNew) = AppSettings.Load();             // Settings.json in Paths.DefaultConfigsDirectory
settings.ServerName = "srv01";
settings.Save();                                        // the previous file is copied to Paths.DefaultBackupDirectory
```

A missing file is created with the default data; a file with an older `Version` is updated and saved again. The directory is
never created automatically: call `Paths.CheckDefaultConfigsDirectory()` (or `Paths.UseLocalAppdataDirectory()`) first.

#### Current settings

The base class has no static instance (an application can use several `JsonSettings<T>` types): to access the settings in
use from any point of the application, the derived class declares its own **static** property. `Load` assigns it with the
instance read (or created), before `Init`, **only while it is null**: once assigned, the settings in use are never replaced
and every following `Load` updates only their properties marked with `[AutoUpdate]` (with the same rules as `AutoUpdate()`:
an invalid file or a refused update keeps the current values and writes a warning to the log), returning the instance in use.
If the file loaded is not `SourceFilePath`, its values are applied but `AutoUpdate()` keeps checking the original file.

- a static property named `Current` is found automatically;
- with another name, the property is marked with `[CurrentSettings]` (it takes precedence over `Current`);
- it must be of the derived type and have a setter of any visibility (`private set` is recommended);
- the internal reads of the file (`CheckTemplate`, `AutoUpdate`) neither assign nor update it, and every other call of `Load`
  does: to read the default file again without changing the current settings, use the overload
  `Load(createIfNotExists, options, doNotAssignCurrent)` with `doNotAssignCurrent: true`. `Init` is executed on the instance
  read in any case, with its side effects (e.g. the log level).

```csharp
public class AppSettings : JsonSettings<AppSettings>
{
    public static AppSettings Current { get; private set; }         // assigned by Load
}

public class DeviceSettings : JsonSettings<DeviceSettings>
{
    [CurrentSettings]
    public static DeviceSettings Instance { get; private set; }     // assigned by Load
}

AppSettings.Load();
DeviceSettings.Load("Device.json");
var server = AppSettings.Current.ServerName;

// Reads the default file again to modify and save it, while AppSettings.Current keeps referring to the instance in use
var copy = AppSettings.Load(false, null, doNotAssignCurrent: true).Object;
```

#### Automatic update

`AutoUpdate()` reads the file again when its modification date changes and applies to the instance in use (updated in place,
never replaced, so `Current` and every reference remain valid) only the properties marked with `[AutoUpdate]`; the others are
applied after a restart. `StartAutoUpdate(intervalSeconds = 1)` calls it with a timer, `StopAutoUpdate()` stops it.

```csharp
public class AppSettings : JsonSettings<AppSettings>
{
    public static AppSettings Current { get; private set; }

    [AutoUpdate]
    public LogLevel LogLevel { get; set; }              // applied as soon as the file is modified

    public string ListenAddress { get; set; }           // applied only after a restart

    public override void Init() => SimpleLog.LogLevel = LogLevel;    // executed again at every update: must be repeatable
    ...

    // Optional: special cases (adjust the instance read, apply unmarked properties, refuse the update)
    protected override bool AutoUpdateCustomLogic(AppSettings loaded, List<string> problems) => true;

    // Optional: reasons of an invalid file, in place of the generic "The file ... is not valid."
    protected override void AutoUpdateInvalidFileProblems(AppSettings loaded, List<string> problems)
        => problems.AddRange(loaded.Errors);            // e.g. errors collected by CheckIsValid
}

AppSettings.Load();                                     // at startup
AppSettings.Current.AutoUpdated += (sender, result) =>  // optional: raised by the timer when something happened
{
    if (result.Problems.Count > 0 && result.FileChanged)
    {
        // e.g. send a warning: FileChanged is true only at the first read of each version of the file
    }
};
AppSettings.Current.StartAutoUpdate();                  // checks the file every second
...
AppSettings.Current.StopAutoUpdate();                   // at shutdown
```

`AutoUpdate` never throws. The outcome (`Status`) is `NoChange`, `Applied`, `Rejected` (file missing, unreadable or not
valid, refused by `AutoUpdateCustomLogic`, or invalid once applied: the current values are kept and the file is read again at
every call until it is corrected) or `FileBusy` (file locked, e.g. still being saved: retried at the next call). For a file
not valid, `Problems` contains the reasons given by `AutoUpdateInvalidFileProblems` (by default a generic message). `Init` is
executed again on the instance in use after every applied or rejected update. The application's own `Save` on the same file
is not detected as a modification.

The timer of `StartAutoUpdate` runs again `intervalSeconds` after the end of each check (no overlaps), writes to the log the
properties applied and the new problems, and raises `AutoUpdated` (on a thread-pool thread) for every outcome other than
`NoChange`. `AutoUpdate()` can also be called directly, e.g. in the main loop of a service, without the timer.

#### Migration from an INI file

`MigrateFromIniFile()` converts the `Settings.ini` of an application that used `IniFileHelper` into the JSON file of a
`JsonSettings<T>`. Call it at every startup before `Load`: once the JSON file exists it does nothing.

```csharp
public class AppSettings : JsonSettings<AppSettings>
{
    public string ListenAddress { get; set; }   // from the key LISTENADDRESS = ...
    public List<string> Hosts { get; set; }     // from the array *HOSTS = ...
    public string Password { get; set; }        // the INI key had a different name: handled by the custom logic

    protected override bool MigrateFromIniFileCustomLogic(IniFileHelper iniFile, ICollection<string> notMigratedProperties,
        List<string> problems)
    {
        Password = iniFile.GetString("PWD");
        notMigratedProperties.Remove(nameof(Password));
        return true;                            // false cancels the migration
    }
    ...
}

var migration = AppSettings.MigrateFromIniFile();   // Settings.ini -> Configs/Settings.json
if (migration.Status == JsonSettingsMigrationStatus.Failed)
{
    // log migration.Problems and stop: Load would create a default file in place of the INI settings
}
AppSettings.Load();
```

- Paths: by default the INI file is `IniFileHelper.INIFILEFULLPATH` (application folder) or, if missing,
  `Paths.DefaultSettingsFile`; the JSON file is `Settings.json` in `Paths.DefaultConfigsDirectory`. Both can be passed as
  parameters; `overwrite: true` replaces an existing JSON file (a backup copy is saved first).
- Default conversion: each public read/write property (not `[JsonIgnore]`, `Version` excluded) gets the key with the same name,
  not case sensitive. Supported: `string`, `bool` (TRUE/FALSE/1/0), integer types, `decimal`/`double`/`float`, `DateTime`,
  `TimeSpan`, `Guid` (invariant culture: `1.5`, `2026-12-31`), enums (name or number) and their nullable forms; arrays,
  `List<X>` and the generic collection interfaces of these types from the INI array with the same name; collections of
  `DayOfWeek` also from a single key such as `1 3 5`.
- Properties missing in the INI file, of other types, with values that cannot be converted, or ambiguous (names that differ only
  in case) keep the default value of `CreateDefaultData` and are listed in `NotMigratedProperties`; the custom logic can handle
  them and remove them from the list.
- Outcome (`Status`): `Migrated`, `MigratedWithWarnings` (properties not migrated or `Problems` left), `DestinationExists`,
  `SourceNotFound`, `Failed` (invalid INI file or path, refused by the custom logic, I/O error: no file written). The method
  never throws and writes the outcome to the log.
- The INI file is not modified and its pragmas (`!!DEBUG!!`, …) are not applied. The instance is neither initialised nor
  assigned to `Current`: `Load` does it. Passwords in plain text are written in plain text (`OSKHelpers.Mail` encrypts them at
  the first `MailWorker.Setup<T>`).

### Data in the Appdata directory

`Paths.MigrateToAppdata()` moves the data of an application from the old structure, directly in the application folder, to
`<application folder>/Appdata`, then uses it as the base of the default directories and creates them, like
`Paths.UseLocalAppdataDirectory()`, which it replaces at start-up.

```csharp
using OSKHelpers.Common;

var migration = Paths.MigrateToAppdata(new[] { "Templates" });  // optional data items of the application
if (migration.Status == PathsMigrationStatus.Failed)
{
    // the data is still in the application folder, which is still the base of the default directories
}
```

- Moved: `Backups`, `Configs`, `Database`, `Logs`, `Output`, `Scripts`, `Temp`, `Settings.ini` and the additional items, which
  are paths relative to the application folder (absolute paths, `..` and `Appdata` are ignored and reported). The files
  distributed with the application (`SettingsTemplate.ini`, `MailTemplates`, binaries) stay where they are.
- A directory missing in `Appdata` is moved as a whole, otherwise its content is merged file by file. A file already in
  `Appdata` is kept and the old one is left in place and listed in `SkippedItems`; `overwrite: true` replaces it. A file that
  cannot be moved (e.g. in use) stays in place and is reported in `Problems`; it is moved at a later start.
- Outcome (`Status`): `Migrated`, `MigratedWithWarnings` (`SkippedItems` or `Problems`), `NothingToMigrate` (already migrated or
  new installation), `NotApplicable` (container: `/app/appdata` is already the base), `Failed` (the `Appdata` directory cannot be
  created: nothing moved, base unchanged). `MovedItems`, `SourceDirectory` and `DestinationDirectory` complete the result.
- The method never throws and can stay in the code: once the migration is done it only activates `Appdata`. It writes the
  outcome to the log only after the new base is active, so the old `Logs` folder is not created again.
- Call it **first**, before any use of `SimpleLog`, `IniFileHelper`, `JsonSettings<T>`, `CredentialProtector` or `MailWorker`:
  they compute or use the default paths. The moved files keep their content and permissions, and DPAPI-encrypted values do not
  depend on the path.

#### Migrating an application to Appdata and JSON settings

Steps for an application that keeps its data in the application folder and reads `Settings.ini` with `IniFileHelper`.

Development:

1. Update `OSKHelpers` (and `OSKHelpers.Mail`, if used) to a version that includes `Paths.MigrateToAppdata()` and
   `JsonSettings<T>.MigrateFromIniFile()`.
2. Create the settings class from the `Json/Settings.cs` template, with one property for each key of `Settings.ini`, with the
   same name (case does not matter) and a matching type (see "Migration from an INI file").
3. Handle keys with a different name or an unsupported type in `MigrateFromIniFileCustomLogic`, removing the handled properties
   from `notMigratedProperties`; remove in the same way the new properties whose template default is correct.
4. Replace the `IniFileHelper` getters with the properties of the class, and `SimpleLog.SetLogLevel(iniFile)` with
   `SimpleLog.SetLogLevel(settings)`.
5. Replace `Paths.UseLocalAppdataDirectory()` with `Paths.MigrateToAppdata()`, listing the data items of the application that
   live in its folder.

Start-up sequence:

```csharp
// 1. Data structure: first of all, before any log or settings access
var pathsMigration = Paths.MigrateToAppdata(new[] { "Templates" });
if (pathsMigration.Status == PathsMigrationStatus.Failed)
{
    // stop, or go on with the old structure
}

// 2. Settings: Appdata/Settings.ini -> Appdata/Configs/Settings.json
var settingsMigration = AppSettings.MigrateFromIniFile();
if (settingsMigration.Status == JsonSettingsMigrationStatus.Failed)
{
    // stop: Load would create a default file in place of the INI settings
}

// 3. Settings and log level
var (settings, isNew) = AppSettings.Load();
SimpleLog.SetLogLevel(settings);
```

- Order: data structure first, so that `MigrateFromIniFile` finds `Settings.ini` in `Paths.DefaultSettingsFile`, writes
  `Settings.json` directly in the new `Configs` folder and every log line goes to the new `Logs` folder.
- First start: `Migrated` (or `MigratedWithWarnings`) for both migrations; later starts: `NothingToMigrate` and
  `DestinationExists`, with no change. New installation: `NothingToMigrate` and `SourceNotFound`; `Load` creates the default
  JSON file.

After the first start:

1. Check in `Appdata/Logs` the outcome of both migrations: skipped items, properties not migrated, problems.
2. Check the values in `Appdata/Configs/Settings.json`, in particular those set by the custom logic. Passwords in plain text in
   the INI file are in plain text in the JSON file too, unless `MailWorker.Setup<T>` encrypts them.
3. No data directory must be left in the application folder: fix by hand any item left there (conflict or file in use), which is
   moved at the next start.
4. `Settings.ini` stays in `Appdata` and is no longer read: delete it once the JSON settings are verified. While it exists,
   `MigrateFromIniFile(overwrite: true)` can repeat the conversion.

Deployment:

- Installation and update packages must no longer contain `Settings.ini` in the application folder: it would be found as an
  item to migrate, in conflict with the one already moved, and `IniFileHelper` would prefer it to the one in `Appdata`.
  `SettingsTemplate.ini` can stay.
- Update the backup and deployment scripts that refer to `Logs`, `Configs`, `Database`, … in the application folder.

The two steps can also be done at different times: with only `Paths.MigrateToAppdata()` at start-up, `IniFileHelper` still
finds `Settings.ini` in `Appdata`, and the JSON migration (step 2) can be added later. The opposite order works as well:
`Settings.json` is moved together with `Configs`.

### Password hashing

```csharp
using OSKHelpers.Security;

byte[] hash = PasswordHelper.HashPassword(password);    // store it (49 bytes)

if (PasswordHelper.VerifyPassword(password, storedHash) && PasswordHelper.RequiresRehash(storedHash))
{
    storedHash = PasswordHelper.HashPassword(password); // upgrade a hash created by OSKHelpers 1.x
}
```

### Encrypting stored credentials

```csharp
using OSKHelpers.Security;

// One instance per application; the entropy must never change once values are encrypted.
var protector = new CredentialProtector("MyApplication.Credentials");

// Without an entropy, a random one is read from __credentials.entropy in Paths.DefaultConfigsDirectory (created on first use;
// deleting it makes the encrypted values no longer decryptable). CreateOrReadEntropyFile(path) reads or creates any other file.
var defaultProtector = new CredentialProtector();

// A whole value: "secret" -> "DPAPI:AQAAANCM..." (Windows) or "AES:..." (Linux, key file in Paths.DefaultDatabaseDirectory)
if (CredentialProtector.RequiresProtection(settings.Password))
{
    settings.Password = protector.Protect(settings.Password);
}
var password = protector.Reveal(settings.Password);

// A value embedded in a string, between [PWD] and [/PWD] (tags configurable in the constructor)
var stored = protector.ProtectTagged("Server=db;User=SA;Password=[PWD]secret[/PWD];");   // ...Password=[PWD]DPAPI:...[/PWD];
var usable = protector.RevealTagged(stored);                                              // Server=db;User=SA;Password=secret;
```

---

## Conventions

- **Errors**: most methods catch their exceptions, log them with `SimpleLog.LogError` and return `false` or an empty value.
  The documented exceptions are argument checks and the file operations of `JsonSettings<T>.Load`/`Save` and
  `WebDownloader`, which are intentionally not caught.
- **Static state**: `SimpleLog`, `Paths` and `OSKHttpClient` keep process-wide settings: the last assignment wins.
- **Default directories**: `Paths` computes all of them from `AppdataDirectory` (the application folder, `/app/appdata` in a
  container). `Paths.UseLocalAppdataDirectory()` moves them to `<application folder>/Appdata` and creates them;
  `Paths.MigrateToAppdata()` does the same after moving the existing data there. Loggers without an explicit `LogPath` follow
  the change.
- **Synchronous counterparts**: every public asynchronous method has a synchronous version with the same name without
  `Async`.
- **Debug builds**: a library built in Debug starts with log level `Debug`, compiles the `Settings.cs` templates and exposes some
  members used only by the tests; the published packages are built in Release.

---

## Namespaces & Classes

### `OSKHelpers.CollectionUtils`
Utilities for paginating collections.

| Class / Enum | Description |
|---|---|
| `Paging` | Calculates pagination metadata (pages, groups, borders) for a given element count and page size. |
| `PagingCollection<T>` | Generic wrapper that holds a `List<T>` together with all pagination state, making it easy to expose paged data directly from a view-model or API response. |
| `PagingControlChars` | Enum whose negative values represent special navigation actions (First, Previous, Next, Last, FastRewind, FastForward, SuspensionPoints) for building pager UI controls. |

---

### `OSKHelpers.Common`
A broad set of stand-alone utility classes covering everyday programming tasks.

| Class | Description |
|---|---|
| `DateUtils` | Converts `DateTime?` to/from string using a configurable format (default `yyyy-MM-dd HH:mm:ss:ffff`). |
| `EmbeddedResourcesUtils` | Extracts a named embedded resource from the calling assembly and saves it to disk; a missing resource throws `InvalidOperationException`. |
| `GCUtils` | Helpers for the Garbage Collector: set a hard heap limit (.NET 8+) and force a partial or aggressive collection. |
| `KeyValue<TKey, TValue>` | Strongly typed key/value pair — ideal for populating combo-boxes and similar UI elements. |
| `MailUtils` | Regex-based e-mail address validator: checks whether a string is a valid address, contains one or more valid addresses, or contains exactly one address. |
| `NetUtils` | Performs a DNS lookup (`CheckDNS` / `CheckDNSAsync`) and returns reachability status together with the resolved `IPAddress` array; an unknown host returns `Reachable = false`. |
| `ObjectUtils` | Dumps all public properties of any object to the console and/or the log (`Dump<T>`). Includes an `IsNumeric` type check. |
| `OSKEnvironment` | Static flags for the current runtime environment: `IsWindows`, `IsMacOS`, `IsLinux`, `IsInteractive`, `IsDockerized`. |
| `OSKFile` | Opens an external file via the OS shell (`Process.Start`). |
| `OSKIPAddress` | Extends `IPAddress` with regex-based extraction of an IPv4 quad (`Match`) and a combined parse-and-validate method (`TryParseMatch`). |
| `OSKRandom` | Extends `Random` with a tick-based seed and a thread-safe static `Shared` instance, mirroring the .NET 6+ `Random.Shared` pattern. |
| `Paths` | Centralises the filesystem paths: `AssemblyPath`, `AppDomainBaseDirectory`, `AppContextBaseDirectory`, `AppdataDirectory` and the default directories computed from it (`DefaultConfigsDirectory`, `DefaultBackupDirectory`, `DefaultDatabaseDirectory`, `DefaultLogsDirectory`, `DefaultOutputDirectory`, `DefaultScriptsDirectory`), container-aware; `Check...Directory` methods to create them; `UseLocalAppdataDirectory` and `MigrateToAppdata` to use `<application folder>/Appdata`, the latter moving the existing data there; temporary files (`GetNewTempFilename`) with an optional cleanup timer for stale files. |
| `PathsMigrationResult`, `PathsMigrationStatus` | Outcome of `Paths.MigrateToAppdata()`: status, source and destination directories, items moved and skipped, problems. |
| `StringBools` | Represents an array of booleans as a compact string of `'1'`/`'0'` characters, with serialisation, indexing, and aggregate helpers (`AnyTrue`, `AnyFalse`). |
| `StringUtils` | Rich string manipulation: ASCII normalisation, regex-based character-class validation, random string generation, and more. |
| `ThreadUtils` | Runs a `Task` on a dedicated `Thread` wrapped in a try/catch that logs any unhandled exception via `SimpleLog`. |
| `WebUtils` | Validates whether a string is a well-formed absolute URI, with optional scheme restrictions (HTTP-only, HTTPS-only, or either). |

---

### `OSKHelpers.Diagnostics`
Utilities to be used for diagnostics.

| Class | Description |
|---|---|
| `MemoryDiagnostics` | Utility methods for memory usage diagnostics during program execution. |

---

### `OSKHelpers.Docker`
Utilities for container-aware applications.

| Class | Description |
|---|---|
| `DockerUtils` | Detects at startup whether the process is running inside a Docker container by checking for the `/.dockerenv` sentinel file (`IsDockerized`); `CheckIsDockerized()` repeats the check on demand. |

---

### `OSKHelpers.ExtensionMethods`
Extension methods that expose library functionality directly on built-in types.

| Class | Description |
|---|---|
| `StringExtensionMethods` | Adds `AsASCII(…)` to `string`, delegating to `StringUtils`. |
| `ObjectExtensionMethods` | Adds `OSKDump(…)` and `OSKIsNumeric()` to every `object`, delegating to `ObjectUtils`. |
| `IPAddressExtensionMethods` | Adds `Match(address)` and `TryParseMatch(address, out ip)` to `IPAddress`, delegating to `OSKIPAddress`. |

---

### `OSKHelpers.INIFile`
Reading and writing INI-style configuration files.

| Class | Description |
|---|---|
| `IniFileHelper` | Full-featured INI parser: reads key/value pairs and arrays, supports type-safe getters (`GetString`, `GetInt`, `GetBool`, …, and `TryGetInt`/`TryGetBool` to detect invalid values), automatic debug/protocol log-level overrides via special markers, and lazy loading. With the default path the INI file is looked up in the application folder and then in `Paths.AppdataDirectory`. |
| `DaysOfWeekParameter` | Parses a space-separated list of integer day codes (0 = Sunday … 6 = Saturday) into a typed `IReadOnlyList<DayOfWeek>`. |

---

### `OSKHelpers.Interfaces`
Interfaces shared by the settings classes.

| Interface | Description |
|---|---|
| `ISettingsBase` | Base settings interface (product name, version and date, `LogLevel`), compatible with `IniFileHelper` and `JsonSettings<T>`; used by `SimpleLog.SetLogLevel(settings)`. |

---

### `OSKHelpers.Json`
JSON configuration and serialisation helpers.

| Class | Description |
|---|---|
| `JsonSettings<T>` | Abstract base class for JSON-backed configuration files. Supports versioning with automatic migration to newer property sets, configurable serialisation options (`DefaultSerializerOptions`, distinct for each derived type), timestamped backups on save, and a configurable `LASTVERSION` guard. The settings in use are exposed by a static property of the derived class (`Current` or `[CurrentSettings]`), assigned by `Load`, and updated while the application is running by `AutoUpdate()` or by the timer of `StartAutoUpdate()`/`StopAutoUpdate()` (event `AutoUpdated`). `MigrateFromIniFile()` converts a `Settings.ini` into the JSON file. |
| `AutoUpdateAttribute` | Marks the properties of a `JsonSettings<T>` applied by `AutoUpdate()` when the file is modified. |
| `CurrentSettingsAttribute` | Marks the static property of a `JsonSettings<T>` assigned by `Load` with the current settings, when it is not named `Current`. |
| `JsonSettingsAutoUpdateResult`, `JsonSettingsAutoUpdateStatus` | Outcome of `JsonSettings<T>.AutoUpdate()`: status, file changed or rewritten, properties changed, problems. |
| `JsonSettingsMigrationResult<T>`, `JsonSettingsMigrationStatus` | Outcome of `JsonSettings<T>.MigrateFromIniFile()`: status, instance written, paths, properties migrated and not migrated, problems. |
| `JsonUtils` | Serialises any object using its runtime type (instead of the declared compile-time type), with configurable `JsonSerializerOptions`. |

---

### `OSKHelpers.Logging`
Lightweight, file-based application logging.

| Class / Interface / Enum | Description |
|---|---|
| `LogLevel` | Enum with six levels: `None`, `Error`, `Warning`, `Info`, `Debug`, `Protocol`. |
| `SimpleLogger` | Core logger: writes timestamped messages to a daily log file in `LogPath` (by default `Paths.DefaultLogsDirectory`, following its changes). Supports console output, prefix-based file naming, forced debug/protocol modes, configurable `LogLevel` and the optional monthly archiving of the old log files (`EnableArchiving`, `ArchiveRetentionMonths`, `ArchiveLogs`). |
| `SimpleLog` | Static façade over a shared `SimpleLogger` instance. Use this for zero-configuration, application-wide logging. |
| `ICSVLogItem` | Interface that objects must implement to be logged via `CSVLogger<T>`: `GetCSVHeader()` and `GetCSVData()`. |
| `CSVLogItem` | Base implementation of `ICSVLogItem` that auto-generates header and data rows from all public properties (sorted alphabetically) via reflection. |
| `CSVLogger<T>` | Generic logger that writes instances of `T` (where `T : ICSVLogItem`) to a daily CSV file, writing the header at the start of each new file. |

---

### `OSKHelpers.Net`
HTTP client helpers.

| Class | Description |
|---|---|
| `OSKHttpClient` | Thread-safe lazy singleton wrapping `HttpClient`. Supports optional acceptance of insecure (self-signed) connections via `AcceptInsecureConnections` and exposes `GetInsecureHttpHandler()`. |
| `WebDownloader` | Downloads a remote file to disk, either synchronously (`Download`) or asynchronously (`DownloadAsync`), using a provided `HttpClient` instance. Optional `CancellationToken` and inactivity timeout (maximum time without receiving data, `TimeoutException` when exceeded); an interrupted download deletes the partial file. |

---

### `OSKHelpers.ODBC`
Utilities for ODBC data-source enumeration (Windows only).

| Class | Description |
|---|---|
| `Utils` | Reads the system and 32-bit ODBC data-source names from the Windows Registry (`GetODBCSources`) and builds a DSN connection string (`GetConnectionString`). Falls back gracefully on non-Windows platforms. |

---

### `OSKHelpers.Security`
Password hashing, credential encryption and obfuscation.

| Class | Description |
|---|---|
| `CredentialProtector` | Encrypts credentials stored in configuration files: DPAPI (`LocalMachine` scope, `DPAPI:` prefix) on Windows, AES with a random 256-bit key file readable only by its owner on other systems (AES-GCM `AES:` with .NET 8, AES-CBC + HMAC-SHA256 `AESCBC:` with .NET Standard 2.0). A value without prefix is plain text (`RequiresProtection`, `Protect`, `TryReveal`, `Reveal`). Credentials embedded in a string, e.g. a connection string, are handled between configurable tags, default `[PWD]` / `[/PWD]` (`ProtectTagged`, `TryRevealTagged`, `RevealTagged`). The entropy passed to the constructor must not change once values are encrypted; without an entropy, a random one is read from `__credentials.entropy` in `Paths.DefaultConfigsDirectory`, created on first use (`CreateOrReadEntropyFile`). |
| `Hash` | Static helper to compute SHA-256 or SHA-512 hashes of a string, not safe for use with passwords (`ComputeSha256Hash`, `ComputeSha512Hash`). |
| `PasswordHelper`| Static helper to create and check hashes safe to store passwords, and to detect legacy hashes to regenerate (`RequiresRehash`). |
| `PasswordHash` | PBKDF2/RFC-2898 password hasher (HMAC-SHA256, 600 000 iterations, random 16-byte salt; 49-byte versioned output), suitable for storing credentials in a database. Legacy 36-byte hashes (HMAC-SHA1, 10 000 iterations) are still verified and flagged by `RequiresRehash`. |
| `PasswordObfuscator` | Obfuscates a password in memory using a randomly seeded byte array, to reduce plaintext exposure during the process lifetime (`EncodePassword` / `DecodePassword`); thread-safe. |

---

### `OSKHelpers.Types.IsChanged`
Infrastructure for change-tracking on model objects.

| Class / Interface | Description |
|---|---|
| `IIsChanged` | Marker interface with an `IsChanged` flag and a `ResetIsChangedExecute()` hook for deep resets. |
| `IIsChangedExtensionMethods` | Extension methods for `IIsChanged`: `SetProperty<T>` sets a backing field, triggers optional pre/post callbacks, and automatically sets `IsChanged = true`. `SetIsChanged` and `ResetIsChanged` manage the flag directly. |
| `PropertyStringUtils` | Converts user-entered strings (from web inputs such as Blazor/MVC forms) to typed backing fields, respecting a configurable `CultureInfo`, `DecimalSeparator`, and `ThousandsSeparator`. |

---

### Content files

The package adds to the project that installs it, in the `.OSKHelpers.content` folder, templates to copy and adapt:

| File | Description |
|---|---|
| `Settings.cs` | `Settings` class based on `IniFileHelper` (namespace `OSKHelpers.Templates.INIFile`). |
| `SettingsTemplate.ini` | Commented INI settings file. |
| `Json/Settings.cs` | `Settings` class based on `JsonSettings<T>` (namespace `OSKHelpers.Templates.Json`). |

---

## Changes in 2.1.3
- `JsonSettings<T>`: automatic update of the settings in use when the file is modified while the application is running, for
  the properties marked with the new `[AutoUpdate]` attribute: `AutoUpdate()`, timer `StartAutoUpdate`/`StopAutoUpdate`, event
  `AutoUpdated`, overridable `AutoUpdateCustomLogic` and `AutoUpdateInvalidFileProblems` (reasons of an invalid file), result
  `JsonSettingsAutoUpdateResult` (see "Settings in a JSON file").
- `JsonSettings<T>.Load` assigns the static property of the current settings declared by the derived class, named `Current` or
  marked with the new `[CurrentSettings]` attribute, before `Init`, only while it is null: once assigned, `Load` updates only
  its properties marked with `[AutoUpdate]` and returns the instance in use; the new overload
  `Load(createIfNotExists, options, doNotAssignCurrent)` reads the default file without assigning or updating it, while the
  existing signatures of `Load` do not change. **Note**: a derived class that already declares a static `Current` property of
  its own type now gets it assigned by `Load`, and a second `Load` no longer replaces it.
- `JsonSettings<T>`: new `MigrateFromIniFile` to create the JSON file from `Settings.ini`, with default conversion of the types,
  overridable `MigrateFromIniFileCustomLogic` and result `JsonSettingsMigrationResult<T>`.
- `Paths`: new `MigrateToAppdata` to move the default directories, `Settings.ini` and the additional items of the application
  from `AssemblyPath` to `<AssemblyPath>/Appdata` and activate the new base (result `PathsMigrationResult`); it replaces
  `UseLocalAppdataDirectory` at startup (see "Data in the Appdata directory").
- `IniFileHelper`: with the default path, `Settings.ini` is looked for in `AssemblyPath` and, if missing, in
  `Paths.AppdataDirectory`; new `TryGetBool` and `TryGetInt`.

## Changes in 2.1.2
- `CredentialProtector`: the constructor accepts a null entropy, read from `__credentials.entropy` in
  `Paths.DefaultConfigsDirectory` and created on first use with 32 random bytes (mode `600` on systems other than Windows); new
  public `CreateOrReadEntropyFile(path)` and constants `DEFAULTENTROPYFILENAME`, `DEFAULTENTROPYSIZE`.

## Changes in 2.1.1
- `IniFileHelper.Save` keeps the layout of the existing file (comments, pragmas, key names, alignment), using
  `SettingsTemplate.ini` only when the file does not exist yet, and no longer requires the template; removed keys and arrays
  (new `Remove` method) are dropped, new ones are appended at the end. Errors are logged and `Save` returns `false`.

## Changes in 2.1.0

- New `Security.CredentialProtector`: encrypts credentials stored in configuration files, with DPAPI on Windows and AES with a
  key file (mode `600`) on other systems: AES-GCM (`AES:` prefix) with .NET 8, AES-CBC with HMAC-SHA256 (`AESCBC:` prefix)
  with .NET Standard 2.0; the .NET 8 build decrypts both formats. Credentials embedded in a string (e.g. a connection string)
  can be encrypted and revealed between configurable tags (default `[PWD]` / `[/PWD]`).
- New dependency: `System.Security.Cryptography.ProtectedData` 8.0.0.
- `SimpleLogger` and `SimpleLog`: optional monthly archiving of the old log files into `<Prefix>Log_<yyyy>_<MM>.zip`
  (`EnableArchiving`, default off; `ArchiveRetentionMonths`, default 1; `ArchiveLogs()`), see
  [Archiving of the old logs](#archiving-of-the-old-logs).
- `CSVLogItem`: the .NET Standard 2.0 build writes `DateOnly` properties in ISO format (`yyyy-MM-ddT00:00:00`) like the .NET 8
  build, instead of a quoted string in the current culture: update any tool that reads those CSV files.
- No other breaking changes.

## Changes in 2.0.1

- `WebDownloader.Download` and `DownloadAsync` accept an optional `CancellationToken` and an optional inactivity timeout
  (maximum time without receiving data, restarted at every block received): cancellation throws
  `OperationCanceledException`, an expired timeout throws `TimeoutException`. An interrupted download deletes the partial
  output file.
- Source compatible with 2.0.0; assemblies compiled against 2.0.0 that call `WebDownloader` must be recompiled, because the
  method signatures changed.

---

## Breaking changes in 2.0.0

Compared with **1.7.5**. Most entries are bug fixes, but they change results that existing code may rely on.

### Changes that require attention

| Area | 1.7.5 | 2.0.0 | What to do |
| --- | --- | --- | --- |
| `PasswordHash`, `PasswordHelper` | PBKDF2-HMAC-SHA1, 10,000 iterations, `ToArray()` of 36 bytes, `Hash` of 20 bytes | PBKDF2-HMAC-SHA256, 600,000 iterations, versioned `ToArray()` of 49 bytes, `Hash` of 32 bytes | Make room for 49 bytes where hashes are stored (e.g. `varbinary(36)` columns). Existing 36-byte hashes are still verified: regenerate them after a successful login when `RequiresRehash` is true |
| Log folder (`SimpleLogger`, `SimpleLog`, `CSVLogger<T>`) | Read once at the first use of the class; `CSVLogger<T>` always used `<assembly folder>/Logs` | Until `LogPath` is assigned, the current `Paths.DefaultLogsDirectory` is used and followed; assigning `null` or an empty string restores this behaviour | Assign `LogPath` explicitly if a fixed folder is needed. In a container, or after `Paths.UseLocalAppdataDirectory()`, CSV logs are now written in the `Logs` folder of `AppdataDirectory` |
| Log line format | `DebugWrite`, `ProtocolWrite` and `Write(LogLevel, ...)` wrote lines without the level label | Lines contain `DEBUG`/`PROTOCOL`, as the lines written with `Write(text, logLevel)` | Update any tool that parses the log files |
| `ConditionalLog` | Wrote to the file only if the console level was also satisfied and the process was interactive | Console and file thresholds are independent: services and non-interactive processes now write the file | Review the levels passed if fewer lines were expected |
| `PropertyStringUtils` | `CultureInfo` defaulted to `it-IT`, but decimal conversion and `DisplayAsCurrency` used the culture of the thread; an invalid string set a nullable field to `null` | `CultureInfo` defaults to `CurrentCulture` (read at the first use of the class) and is used by all conversions and `DisplayAsCurrency`; an invalid string leaves the field unchanged and `SetProperty` returns `false` | Set `PropertyStringUtils.CultureInfo` explicitly (e.g. `new CultureInfo("it-IT")`) if the result must not depend on the system culture, as on a web server |
| `JsonSettings<T>.Load` | Without options it ignored `DefaultSerializerOptions` | Without options it uses `DefaultSerializerOptions` (as `Save` already did) | Check custom `DefaultSerializerOptions`: they now apply to reading too |
| `NetUtils.CheckDNS`/`CheckDNSAsync` | Threw `SocketException` for a host that cannot be resolved | Returns `Reachable = false` with an empty address list | Remove `try/catch` blocks used to detect unreachable hosts |
| `EmbeddedResourcesUtils.ExtractEmbeddedFile` | Did nothing if the resource did not exist | Throws `InvalidOperationException` | Handle the exception where the resource may be missing |
| `DaysOfWeekParameter.Add`/`Remove` | Threw `ArgumentNullException` for an invalid day | Throw `ArgumentOutOfRangeException` | Update `catch` blocks |
| `CSVLogItem.GetCSVData` | A `null` property produced `#ERR (...)#` and an error in the log | A `null` property produces an empty quoted field (`""`) | Update any tool that reads the CSV files |
| `ObjectExtensionMethods.OSKIsNumeric` | Returned `void` (unusable) | Returns `bool` | Recompile: the signature changed |
| `SimpleLog.ConsoleLog` | Instance method on a class used statically | Static method | Recompile: the signature changed |

### Other behaviour changes

- `StringUtils.SNUMBERS` is `"0123456789"` (the zero was repeated) and `GenerateAlphaNumSpecialsString` really includes the
  special characters.
- `JsonSettings<T>` backup files are named `<Name>_<yyyyMMddHHmmss>[_n]<extension>` (no more double dot before the extension).
- `IniFileHelper.AddArray<T>` adds each value once, also when the list passed contains duplicates; `Parse` no longer modifies
  the list passed and clears the arrays when the file is not valid.
- `ODBC.Utils.GetConnectionString` writes the user name in `Uid` (it used to write the password).
- `GCUtils.SetHeapHardLimit` uses the value passed (it used a fixed 500 MB); `GetCheckTimer` compares the size of the managed
  heap.
- `Paths.CheckDefaultLogsDirectory` checks the logs folder (it checked the database folder).
- `PagingCollection<T>.GetPageElements` returns the last page also when the number of elements is a multiple of the page size.
- `WebDownloader.DownloadAsync` makes a single request and does not create the file for an error status code.
- `ObjectUtils.Dump` and `MemoryDiagnostics` write their labels in English ("Type", "Memory usage"), as do all the library
  messages.
- `IPAddressExtensionMethods.TryParseMatch(string, out IPAddress)` is obsolete: use `OSKIPAddress.TryParseMatch` or the
  `TryParseMatch` extension method on `IPAddress`.

### Non-breaking improvements

- `PasswordHash`: constant-time comparison; no more obsolete `Rfc2898DeriveBytes` constructor on .NET 8.
- `PasswordObfuscator` and `OSKRandom.Shared` (.NET Standard 2.0) are thread-safe; `PasswordObfuscator` no longer fails with
  start indexes near `int.MaxValue`.
- `IIsChangedExtensionMethods.SetProperty` ignores a `null` callback in every overload.
- The `INIFile/Settings.cs` template distributed with the package has English messages.

---

## License

BSD 3-Clause — see the [License](https://github.com/ObySamKenoby/OSKHelpers/blob/main/LICENSE.txt) file.
