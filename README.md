# OSKHelpers

A set of small, general-purpose .NET libraries, each covering a specific need: logging, configuration files, paths,
password hashing, e-mail sending and a number of everyday helpers.

It all started with the need for a simple way to log from inside my programs (NLog and similar frameworks were simply too
much); then other utilities followed: INI and JSON settings files, string and network helpers, reusable types. I use these
libraries in my day-to-day work: they are not meant to compete with full-featured frameworks, but to do what is needed with
as little setup as possible. Suggestions are welcome.

---

## Packages

Each package has its own ReadMe, also shown on its NuGet page, with quick start, list of classes and changes.

| Package | Version | Target | Content | Documentation |
| --- | --- | --- | --- | --- |
| [OSKHelpers](https://www.nuget.org/packages/OSKHelpers) | **2.1.3** | .NET Standard 2.0, .NET 8 | Logging, INI/JSON settings, paths, security, collections, network and generic helpers | [ReadMe](src/OSKHelpers/ReadMe.md) |
| [OSKHelpers.Mail](https://www.nuget.org/packages/OSKHelpers.Mail) | **2.1.0** | .NET Standard 2.0, .NET 8 | E-mail sending (MailKit) with template support | [ReadMe](src/OSKHelpers.Mail/ReadMe.md) |
| [OSKHelpers.Cron](https://www.nuget.org/packages/OSKHelpers.Cron) | **0.9.0** | .NET Standard 2.0 | Scheduling of actions from a crontab file (Cronos), with automatic reload and validation | [ReadMe](src/OSKHelpers.Cron/ReadMe.md) |
| OSKHelpers.WPF | 0.5.0 | .NET Standard 2.0 | Placeholder for WPF helpers: not usable yet | [ReadMe](src/OSKHelpers.WPF/ReadMe.md) |

```
dotnet add package OSKHelpers
dotnet add package OSKHelpers.Mail
dotnet add package OSKHelpers.Cron
```

> **Note**: OSKHelpers.Mail 2.1.0 requires OSKHelpers **2.1.2**, OSKHelpers.Cron 0.9.0 requires OSKHelpers **2.0.0**. When upgrading from 1.x, read the
> [breaking changes of OSKHelpers 2.0.0](src/OSKHelpers/ReadMe.md#breaking-changes-in-200).

> **Breaking changes**: OSKHelpers.Mail 2.1.0 adds `SmtpStartTls` to `ISettingsSmtp` and `ImapStartTls` to `ISettingsImap`, and removes the
> STARTTLS parameter of `SetupSmtp(ISettingsSmtp, ...)` and `Setup<T>`: read the
> [breaking changes of OSKHelpers.Mail 2.1.0](src/OSKHelpers.Mail/ReadMe.md#breaking-changes-in-210) before upgrading.

### What is inside OSKHelpers

| Namespace | Main classes | Purpose |
| --- | --- | --- |
| `Logging` | `SimpleLog`, `SimpleLogger`, `CSVLogger<T>`, `CSVLogItem` | Daily text logs with levels and optional monthly archiving, CSV logs of objects |
| `INIFile` | `IniFileHelper`, `DaysOfWeekParameter` | `KEY = value` settings files, arrays, typed getters |
| `Json` | `JsonSettings<T>`, `JsonUtils` | Versioned JSON settings files with automatic backup, automatic update while running and migration from `Settings.ini` |
| `Common` | `Paths`, `StringUtils`, `DateUtils`, `MailUtils`, `WebUtils`, `NetUtils`, `ObjectUtils`, `OSKRandom`, ... | Default directories, strings, dates, validation, DNS, diagnostics |
| `Security` | `PasswordHelper`, `PasswordHash`, `CredentialProtector`, `PasswordObfuscator`, `Hash` | Password hashing (PBKDF2), encryption of stored credentials (DPAPI/AES), in-memory obfuscation, SHA hashes |
| `Net` | `OSKHttpClient`, `WebDownloader` | Shared `HttpClient`, file download |
| `CollectionUtils` | `Paging`, `PagingCollection<T>` | Pagination and pager controls |
| `Types.IsChanged` | `IIsChanged`, `PropertyStringUtils` | Change tracking on models, conversion of user input |
| `ODBC`, `Docker`, `Diagnostics`, `ExtensionMethods`, `Interfaces` | | ODBC data sources, container detection, memory measurement, extension methods, settings interfaces |

### Example

```csharp
using OSKHelpers.INIFile;
using OSKHelpers.Logging;
using OSKHelpers.Mail;

var iniFile = new IniFileHelper(true);                  // reads Settings.ini in the application folder
SimpleLog.SetLogLevel(iniFile);
SimpleLog.Write("Application started");                 // <application folder>/Logs/Log<yyyyMMdd>.txt

if (MailWorker.SetupSmtp(iniFile))
{
    await MailWorker.SendAsync("to@example.com", "Subject", "<p>HTML body</p>");
}
```

More examples in the [OSKHelpers ReadMe](src/OSKHelpers/ReadMe.md#quick-start) and in the
[OSKHelpers.Mail ReadMe](src/OSKHelpers.Mail/ReadMe.md#quick-start).

---

## Repository

| Folder | Content |
| --- | --- |
| `src` | Library projects, one folder per package with its `ReadMe.md`; `src/Directory.Build.props` holds the package properties shared by all the packages |
| `tests` | MSTest projects (`OSKHelpers.Tests`, `OSKHelpers.Tests.NetStandard`, `OSKHelpers.Mail.Tests`, `OSKHelpers.Cron.Tests`) and an interactive console for manual checks |
| `docs` | Logo of the packages and `Docs.csproj`, which shows the files of the repository root in Visual Studio |

The tests use members available only in Debug builds: run them with `dotnet test -c Debug`. Integration tests use only local
resources and can be excluded with `--filter TestCategory!=Integration`. `OSKHelpers.Tests.NetStandard` runs the same tests on
the .NET Standard 2.0 build of OSKHelpers.

---

## License

BSD 3-Clause, see [LICENSE.txt](LICENSE.txt).
