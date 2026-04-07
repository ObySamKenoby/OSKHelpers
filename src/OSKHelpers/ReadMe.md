# OSKHelpers

**OSKHelpers** is a collection of general-purpose, independent utility classes targeting **.NET Standard 2.0** and **.NET 8**.  
Each namespace addresses a specific concern and has no mandatory coupling with the others, so you can use only what you need.

---

## Namespaces & Classes

### `OSKHelpers.CollectionUtils`
Utilities for paginating collections.

| Class / Enum | Description |
|---|---|
| `Paging` | Calculates pagination metadata (pages, groups, borders) for a given element count and page size. Exposes configurable constants for minimum/default values. |
| `PagingCollection<T>` | Generic wrapper that holds a `List<T>` together with all pagination state, making it easy to expose paged data directly from a view-model or API response. |
| `PagingControlChars` | Enum whose negative values represent special navigation actions (First, Previous, Next, Last, FastRewind, FastForward, SuspensionPoints) for building pager UI controls. |

---

### `OSKHelpers.Common`
A broad set of stand-alone utility classes covering everyday programming tasks.

| Class | Description |
|---|---|
| `DateUtils` | Converts `DateTime?` to/from string using a configurable format (default `yyyy-MM-dd HH:mm:ss:ffff`). |
| `EmbeddedResourcesUtils` | Extracts a named embedded resource from the calling assembly and saves it to disk. |
| `GCUtils` | Helpers for the Garbage Collector: set a hard heap limit (.NET 8+) and force a partial or aggressive collection. |
| `KeyValue<TKey, TValue>` | Strongly typed key/value pair — ideal for populating combo-boxes and similar UI elements. |
| `MailUtils` | Regex-based e-mail address validator: checks whether a string is a valid address, contains one or more valid addresses, or contains exactly one address. |
| `NetUtils` | Performs a DNS lookup (`CheckDNS` / `CheckDNSAsync`) and returns reachability status together with the resolved `IPAddress` array. |
| `ObjectUtils` | Dumps all public properties of any object to the console and/or the log (`Dump<T>`). Includes an `IsNumeric` type check. |
| `OSKEnvironment` | Static flags for the current runtime environment: `IsWindows`, `IsMacOS`, `IsLinux`, `IsInteractive`, `IsDockerized`. |
| `OSKFile` | Opens an external file via the OS shell (`Process.Start`). |
| `OSKIPAddress` | Extends `IPAddress` with regex-based extraction of an IPv4 quad (`Match`) and a combined parse-and-validate method (`TryParseMatch`). |
| `OSKRandom` | Extends `Random` with a tick-based seed and a static `Shared` instance, mirroring the .NET 6+ `Random.Shared` pattern. |
| `Paths` | Centralises commonly needed filesystem paths: `AssemblyPath`, `AppDomainBaseDirectory`, `AppDataDirectory`, temp-file helpers, and automatic temp-folder cleanup via a timer. |
| `StringBools` | Represents an array of booleans as a compact string of `'1'`/`'0'` characters, with serialisation, indexing, and aggregate helpers (`AnyTrue`, `AnyFalse`). |
| `StringUtils` | Rich string manipulation: ASCII normalisation, regex-based character-class validation, random string generation, and more. |
| `ThreadUtils` | Runs a `Task` on a dedicated `Thread` wrapped in a try/catch that logs any unhandled exception via `SimpleLog`. |
| `WebUtils` | Validates whether a string is a well-formed absolute URI, with optional scheme restrictions (HTTP-only, HTTPS-only, or either). |

---

### `OSKHelpers.Docker`
Utilities for container-aware applications.

| Class | Description |
|---|---|
| `DockerUtils` | Detects at startup whether the process is running inside a Docker container by checking for the `/.dockerenv` sentinel file (`IsDockerized`). |

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

### `OSKHelpers.Json`
JSON configuration and serialisation helpers.

| Class | Description |
|---|---|
| `JsonSettings<T>` | Abstract base class for JSON-backed configuration files. Supports versioning with automatic migration to newer property sets, camelCase serialisation, and a configurable `LASTVERSION` guard. |
| `JsonUtils` | Serialises any object using its runtime type (instead of the declared compile-time type), with configurable `JsonSerializerOptions`. |

---

### `OSKHelpers.Logging`
Lightweight, file-based application logging.

| Class / Interface / Enum | Description |
|---|---|
| `LogLevel` | Enum with six levels: `None`, `Error`, `Warning`, `Info`, `Debug`, `Protocol`. |
| `SimpleLogger` | Core logger: writes timestamped messages to a daily rotating log file. Supports console output, prefix-based file naming, forced debug/protocol modes, and configurable `LogLevel`. |
| `SimpleLog` | Static façade over a shared `SimpleLogger` instance. Use this for zero-configuration, application-wide logging. |
| `ICSVLogItem` | Interface that objects must implement to be logged via `CSVLogger<T>`: `GetCSVHeader()` and `GetCSVData()`. |
| `CSVLogItem` | Base implementation of `ICSVLogItem` that auto-generates header and data rows from all public properties (sorted alphabetically) via reflection. |
| `CSVLogger<T>` | Generic logger that writes instances of `T` (where `T : ICSVLogItem`) to a daily rotating CSV file, reusing the same header-rotation strategy as `SimpleLogger`. |

---

### `OSKHelpers.Net`
HTTP client helpers.

| Class | Description |
|---|---|
| `OSKHttpClient` | Thread-safe lazy singleton wrapping `HttpClient`. Supports optional acceptance of insecure (self-signed) connections via `AcceptInsecureConnections` and exposes `GetInsecureHttpHandler()`. |
| `WebDownloader` | Downloads a remote file to disk, either synchronously (`Download`) or asynchronously (`DownloadAsync`), using a provided `HttpClient` instance. |

---

### `OSKHelpers.ODBC`
Utilities for ODBC data-source enumeration (Windows only).

| Class | Description |
|---|---|
| `Utils` | Reads the system and 32-bit ODBC data-source names from the Windows Registry (`GetODBCSources`) and builds a DSN connection string (`GetConnectionString`). Falls back gracefully on non-Windows platforms. |

---

### `OSKHelpers.Security`
Password hashing and obfuscation.

| Class | Description |
|---|---|
| `Hash` | Static helper to compute SHA-256 or SHA-512 hashes of a string (`ComputeSha256Hash`, `ComputeSha512Hash`). |
| `PasswordHash` | PBKDF2/RFC-2898 password hasher with a random 16-byte salt and 10 000 iterations, suitable for storing credentials in a database. |
| `PasswordObfuscator` | Obfuscates a password in memory using a randomly seeded byte array, to reduce plaintext exposure during the process lifetime (`EncodePassword` / `DecodePassword`). |

---

### `OSKHelpers.Types.IsChanged`
Infrastructure for change-tracking on model objects.

| Class / Interface | Description |
|---|---|
| `IIsChanged` | Marker interface with an `IsChanged` flag and a `ResetIsChangedExecute()` hook for deep resets. |
| `IIsChangedExtensionMethods` | Extension methods for `IIsChanged`: `SetProperty<T>` sets a backing field, triggers optional pre/post callbacks, and automatically sets `IsChanged = true`. `SetIsChanged` and `ResetIsChanged` manage the flag directly. |
| `PropertyStringUtils` | Converts user-entered strings (from web inputs such as Blazor/MVC forms) to typed backing fields, respecting a configurable `CultureInfo`, `DecimalSeparator`, and `ThousandsSeparator`. |

---

## License
BSD 3-Clause — see the [LICENSE](License.txt) file.
