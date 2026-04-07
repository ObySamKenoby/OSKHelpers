# OSKHelpers.Mail

**OSKHelpers.Mail** is a lightweight wrapper around [MailKit](https://github.com/jstedfast/MailKit) that simplifies sending and receiving e-mails from .NET Standard 2.0 applications.  
It integrates natively with **OSKHelpers** (logging via `SimpleLog`, configuration via `IniFileHelper`) and provides a template-based message composition system.

---

## Namespaces & Classes

### `OSKHelpers.Mail`

| Class | Description |
|---|---|
| `MailWorker` | Central static class for all mail operations. Manages SMTP and IMAP connection parameters, composes messages from plain text or template files, and sends them asynchronously via MailKit. |

#### SMTP configuration

`MailWorker` exposes a set of static properties for SMTP:

| Property | Description |
|---|---|
| `SmtpServer` | Address of the SMTP server. |
| `SmtpPort` | SMTP server port. |
| `SmtpSSL` | Enables SSL/TLS for the SMTP connection. |
| `SmtpUsername` / `SmtpPassword` | Credentials for SMTP authentication (authentication is disabled when both are empty). |
| `SmtpFrom` | Optional default sender address used when no `from` is specified in a send call. |
| `SmtpAuthEnabled` | `true` when both username and password are set. |
| `SmtpAuthConfigError` | `true` when only one of the two credentials is set (configuration error). |

Configuration can be loaded from an INI file via `SetupSmtp(IniFileHelper)` or set programmatically via `SetupSmtp(server, port, ssl, username, password, from)`.

#### IMAP configuration

| Property | Description |
|---|---|
| `ImapServer` | Address of the IMAP server. |
| `ImapPort` | IMAP server port. |
| `ImapSSL` | Enables SSL/TLS for the IMAP connection. |
| `ImapUsername` / `ImapPassword` | Credentials for IMAP authentication. |
| `ImapAuthConfigError` | `true` when username or password is missing. |

Configuration is loaded from an INI file via `SetupImap(IniFileHelper)` or set programmatically via `SetupImap(server, port, ssl, username, password)`.

#### Sending messages

| Method | Description |
|---|---|
| `SendAsync(recipients, subject, body, attachments)` | Sends an HTML message to one or more recipients with optional stream-based attachments. |
| `SendFromFileAsync(recipients, templateFile, tags, attachments)` | Composes the message body from a template text file, replacing `{TAG}` placeholders with the provided values and extracting the subject from an `@Subject:` line. Supports conditional blocks and comment lines (`#`). |

#### Diagnostics

| Method | Description |
|---|---|
| `DumpStmpParameters` | Prints or logs the current SMTP settings (password is masked). |
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
| [System.Text.Json](https://www.nuget.org/packages/System.Text.Json) | JSON serialisation. |

---

## License
BSD 3-Clause — see the [LICENSE](License.txt) file.