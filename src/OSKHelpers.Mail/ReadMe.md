# OSKHelpers.Mail

**OSKHelpers.Mail** is a lightweight wrapper around [MailKit](https://github.com/jstedfast/MailKit) that simplifies sending e-mails from .NET Standard 2.0 applications (receiving via IMAP is not implemented yet, see below).  
It integrates natively with **OSKHelpers** (logging via `SimpleLog`, configuration via `IniFileHelper`) and provides a template-based message composition system.

---

## Namespaces & Classes

### `OSKHelpers.Mail`

| Class | Description |
|---|---|
| `MailWorker` | Central static class for all mail operations. Manages SMTP and IMAP connection parameters, parses template files, and sends HTML messages (synchronously or asynchronously) via MailKit. |

#### SMTP configuration

`MailWorker` exposes a set of static properties for SMTP:

| Property | Description |
|---|---|
| `SmtpServer` | Address of the SMTP server. |
| `SmtpPort` | SMTP server port. |
| `SmtpSSL` | Enables implicit SSL/TLS for the SMTP connection (e.g. port 465). |
| `SmtpStartTls` | Upgrades the connection to TLS with STARTTLS (e.g. port 587); used only when `SmtpSSL` is `false`. Default `false`: without `SmtpSSL` and `SmtpStartTls` the connection is not encrypted. |
| `SmtpUsername` / `SmtpPassword` | Credentials for SMTP authentication (authentication is disabled when both are empty). |
| `SmtpFrom` | Optional default sender address used when no `from` is specified in a send call. |
| `SmtpAuthEnabled` | `true` when both username and password are set. |
| `SmtpAuthConfigError` | `true` when only one of the two credentials is set (configuration error). |

Configuration can be loaded from an INI file via `SetupSmtp(IniFileHelper)` via `SetupSmtp(ISettingsSmtp, startTls)` or set programmatically via `SetupSmtp(server, port, ssl, username, password, from, startTls)`.
The INI key `SmtpStartTls` is optional (default `false`).

#### IMAP configuration

| Property | Description |
|---|---|
| `ImapServer` | Address of the IMAP server. |
| `ImapPort` | IMAP server port. |
| `ImapSSL` | Enables SSL/TLS for the IMAP connection. |
| `ImapUsername` / `ImapPassword` | Credentials for IMAP authentication (both required). |
| `ImapAuthConfigError` | `true` when username or password is missing. |

Configuration is loaded from an INI file via `SetupImap(IniFileHelper)`, from `ISettingsImap` via `SetupImap(ISettingsImap)`, or set programmatically via `SetupImap(server, port, ssl, username, password)`.

> **ToDo:** receiving e-mails via IMAP is not implemented yet: the library only stores and checks the IMAP parameters (`CheckImapParameters`).

#### Sending messages

| Method | Description |
|---|---|
| `SendAsync(...)` / `Send(...)` | Sends an HTML message to one or more recipients with optional stream-based attachments. Overloads without server parameters use the static SMTP settings, overloads without sender use `SmtpFrom`; the full overload accepts the `startTls` parameter (default `false`). |
| `SendReplyToAsync(...)` / `SendReplyTo(...)` | As above, setting the `Reply-To` addresses. |

#### Templates

| Method | Description |
|---|---|
| `ParseFile(lines, tags, conditions, customTags)` | Processes the lines of a template file (e.g. read with `File.ReadAllLines`): removes comment lines (`#`), extracts the subject from the `@Subject:` line and optional custom `@Tag:` lines, and returns subject and body ready to be passed to `SendAsync`. |
| `ParseString(content, tags, conditions)` | Replaces the `{TAG}` placeholders with the provided values and shows or hides the `<<COND>>...<</COND>>` sections according to their conditions. |

#### Diagnostics

| Method | Description |
|---|---|
| `DumpStmpParameters` | Prints or logs the current SMTP settings (password is masked). |
| `DumpParameters` | Prints or logs both SMTP and IMAP settings. |
| `CheckSmtpParameters` / `CheckImapParameters` | Check that server, port and credentials are present (no connectivity check). |
| `DumpImapParameters` | Prints or logs the current IMAP settings (password is masked). |
| `SetLogLevel` | Sets the `SimpleLog` log level for the library. |

---

## INI configuration template

The package ships a ready-to-use INI template (`SettingsTemplate.Mail.ini`) that documents all supported keys for SMTP and IMAP. Copy it to your project and rename it `Settings.ini` to get started.

---

## Mail templates

The package includes a `MailTemplates/` folder with:

| File | Description |
|---|---|
| `0Index.txt` | Index and usage notes for the template system. |
| `Credentials.txt` | Example template for credential-notification e-mails. |

---

## Dependencies

| Package | Purpose |
|---|---|
| [MailKit](https://www.nuget.org/packages/MailKit) | SMTP/IMAP transport layer. |
| [OSKHelpers](https://www.nuget.org/packages/OSKHelpers) | Logging (`SimpleLog`) and INI configuration (`IniFileHelper`). |
| [System.Security.Cryptography.Pkcs](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs) | Cryptography support required by MimeKit (S/MIME). |

---

## License
BSD 3-Clause — see the [LICENSE](License.txt) file.