# OSKHelpers

A set of small, general-purpose .NET libraries, each covering a specific need: logging, configuration files, paths,
password hashing, e-mail sending and a number of everyday helpers.

It all started with the need for a simple way to log from inside my programs (NLog and similar frameworks were simply too
much); then other utilities followed: INI and JSON settings files, string and network helpers, reusable types. I use these
libraries in my day-to-day work: they are not meant to compete with full-featured frameworks, but to do what is needed with
as little setup as possible. Suggestions are welcome.

---

## Packages

| Package | Version | Target | Content | Documentation |
| --- | --- | --- | --- | --- |
| [OSKHelpers](https://www.nuget.org/packages/OSKHelpers) | **2.0.1** | .NET Standard 2.0, .NET 8 | Logging, INI/JSON settings, paths, security, collections, network and generic helpers | [ReadMe](src/OSKHelpers/ReadMe.md) |
| [OSKHelpers.Mail](https://www.nuget.org/packages/OSKHelpers.Mail) | 1.2.5 | .NET Standard 2.0 | E-mail sending (MailKit) with template support | [ReadMe](src/OSKHelpers.Mail/ReadMe.md) |
| OSKHelpers.WPF | 0.5.0 | — | Placeholder for WPF helpers: not usable yet | — |

> **Note**: OSKHelpers.Mail 1.2.5 is still built against OSKHelpers **1.7.5**. When both packages are used, read the
> [breaking changes](#breaking-changes-in-oskhelpers-200) below before referencing OSKHelpers 2.0.0 explicitly.

```
dotnet add package OSKHelpers
dotnet add package OSKHelpers.Mail
```

---

## What is inside OSKHelpers

| Namespace | Main classes | Purpose |
| --- | --- | --- |
| `Logging` | `SimpleLog`, `SimpleLogger`, `CSVLogger<T>`, `CSVLogItem` | Daily text logs with levels, CSV logs of objects |
| `INIFile` | `IniFileHelper`, `DaysOfWeekParameter` | `KEY = value` settings files, arrays, typed getters |
| `Json` | `JsonSettings<T>`, `JsonUtils` | Versioned JSON settings files with automatic backup |
| `Common` | `Paths`, `StringUtils`, `DateUtils`, `MailUtils`, `WebUtils`, `NetUtils`, `ObjectUtils`, `OSKRandom`, ... | Default directories, strings, dates, validation, DNS, diagnostics |
| `Security` | `PasswordHelper`, `PasswordHash`, `PasswordObfuscator`, `Hash` | Password hashing (PBKDF2), in-memory obfuscation, SHA hashes |
| `Net` | `OSKHttpClient`, `WebDownloader` | Shared `HttpClient`, file download |
| `CollectionUtils` | `Paging`, `PagingCollection<T>` | Pagination and pager controls |
| `Types.IsChanged` | `IIsChanged`, `PropertyStringUtils` | Change tracking on models, conversion of user input |
| `ODBC`, `Docker`, `Diagnostics`, `ExtensionMethods` | | ODBC data sources, container detection, memory measurement, extension methods |

The complete list of classes is in the [OSKHelpers ReadMe](src/OSKHelpers/ReadMe.md).

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

### Sending e-mails (OSKHelpers.Mail)

```csharp
using OSKHelpers.Mail;

MailWorker.SetupSmtp(iniFile);                          // SmtpServer, SmtpPort, SmtpSSL, SmtpStartTls, SmtpUsername, ...
var sent = await MailWorker.SendAsync("to@example.com", "Subject", "<p>HTML body</p>");
```

See the [OSKHelpers.Mail ReadMe](src/OSKHelpers.Mail/ReadMe.md) for templates, attachments and `Reply-To`.

---

## Conventions

- **Errors**: most methods catch their exceptions, log them with `SimpleLog.LogError` and return `false` or an empty value.
  The documented exceptions are argument checks and the file operations of `JsonSettings<T>.Load`/`Save` and
  `WebDownloader`, which are intentionally not caught.
- **Static state**: `SimpleLog`, `Paths`, `OSKHttpClient` and `MailWorker` keep process-wide settings: the last assignment
  wins.
- **Default directories**: `Paths` computes all of them from `AppdataDirectory` (the application folder, `/app/appdata` in a
  container). `Paths.UseLocalAppdataDirectory()` moves them to `<application folder>/Appdata` and creates them; loggers
  without an explicit `LogPath` follow the change.
- **Synchronous counterparts**: every public asynchronous method has a synchronous version with the same name without
  `Async`.

---

## Changes in OSKHelpers 2.0.1

- `WebDownloader.Download` and `DownloadAsync` accept an optional `CancellationToken` and an optional inactivity timeout
  (maximum time without receiving data, restarted at every block received): cancellation throws
  `OperationCanceledException`, an expired timeout throws `TimeoutException`. An interrupted download deletes the partial
  output file.
- Source compatible with 2.0.0; assemblies compiled against 2.0.0 that call `WebDownloader` must be recompiled, because the
  method signatures changed.

---

## Breaking changes in OSKHelpers 2.0.0

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

## Repository

| Folder | Content |
| --- | --- |
| `src` | Library projects |
| `tests` | MSTest projects (`OSKHelpers.Tests`, `OSKHelpers.Mail.Tests`) and an interactive console for manual checks |
| `docs` | Analysis documentation (in Italian): architecture, data model, processes, issue log |

The tests use members available only in Debug builds: run them with `dotnet test -c Debug`. Integration tests use only local
resources and can be excluded with `--filter TestCategory!=Integration`.

---

## License

BSD 3-Clause, see [LICENSE.txt](LICENSE.txt).
