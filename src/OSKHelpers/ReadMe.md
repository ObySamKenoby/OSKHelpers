# OSKHelpers

**OSKHelpers** is a collection of general-purpose, independent utility classes targeting **.NET Standard 2.0** and **.NET 8**.
Each namespace addresses a specific concern and has no mandatory coupling with the others, so you can use only what you need:
logging, INI and JSON settings files, default paths, password hashing and credential encryption, collections, network and
generic helpers.

```
dotnet add package OSKHelpers
```

The e-mail helpers are in the separate [OSKHelpers.Mail](https://www.nuget.org/packages/OSKHelpers.Mail) package. Source code,
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
package.

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
  container). `Paths.UseLocalAppdataDirectory()` moves them to `<application folder>/Appdata` and creates them; loggers
  without an explicit `LogPath` follow the change.
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
| `Paths` | Centralises the filesystem paths: `AssemblyPath`, `AppDomainBaseDirectory`, `AppContextBaseDirectory`, `AppdataDirectory` and the default directories computed from it (`DefaultConfigsDirectory`, `DefaultBackupDirectory`, `DefaultDatabaseDirectory`, `DefaultLogsDirectory`, `DefaultOutputDirectory`, `DefaultScriptsDirectory`), container-aware; `Check...Directory` methods to create them; temporary files (`GetNewTempFilename`) with an optional cleanup timer for stale files. |
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
| `IniFileHelper` | Full-featured INI parser: reads key/value pairs and arrays, supports type-safe getters (`GetString`, `GetInt`, `GetBool`, …), automatic debug/protocol log-level overrides via special markers, and lazy loading. |
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
| `JsonSettings<T>` | Abstract base class for JSON-backed configuration files. Supports versioning with automatic migration to newer property sets, configurable serialisation options (`DefaultSerializerOptions`, distinct for each derived type), timestamped backups on save, and a configurable `LASTVERSION` guard. |
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
