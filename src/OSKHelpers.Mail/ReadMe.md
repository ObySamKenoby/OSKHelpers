# OSKHelpers.Mail

**OSKHelpers.Mail** is a lightweight wrapper around [MailKit](https://github.com/jstedfast/MailKit) that simplifies sending
HTML e-mails from **.NET Standard 2.0** applications (receiving via IMAP is not implemented yet, see below).
It integrates with [OSKHelpers](https://www.nuget.org/packages/OSKHelpers) (logging via `SimpleLog`, configuration via
`IniFileHelper`) and provides a template-based message composition system.

```
dotnet add package OSKHelpers.Mail
```

Source code, issue tracking and the other packages:
[github.com/ObySamKenoby/OSKHelpers](https://github.com/ObySamKenoby/OSKHelpers).

> **Compatibility**: OSKHelpers.Mail 2.0.0 requires OSKHelpers **2.1.0**: when upgrading from 1.x read the
> [breaking changes of OSKHelpers 2.0.0](https://github.com/ObySamKenoby/OSKHelpers/blob/main/src/OSKHelpers/ReadMe.md#breaking-changes-in-200).
> The package contains two builds, one for each MailKit major version (see [MailKit versions](#mailkit-versions)).

---

## Quick start

```ini
# Settings.ini
SmtpServer      = smtp.example.com
SmtpPort        = 587
SmtpSSL         = False
SmtpStartTls    = True
SmtpUsername    = sender@example.com
SmtpPassword    = mySmtpPassword
SmtpFrom        = Sender<sender@example.com>
```

```csharp
using OSKHelpers.INIFile;
using OSKHelpers.Mail;

var iniFile = new IniFileHelper(true);
if (MailWorker.SetupSmtp(iniFile))
{
    var sent = await MailWorker.SendAsync("to@example.com", "Subject", "<p>HTML body</p>");
}
```

With a template file:

```csharp
var lines   = File.ReadAllLines("MailTemplates/Credentials.txt").ToList();
var tags    = new List<(string Name, string Value)> { ("FULLNAME", "Mario Rossi"), ("USERNAME", "mrossi") };
var conds   = new List<(string Name, bool Check)> { ("ISNEW", true), ("ISNOTNEW", false) };

var (parsed, subject, body, _) = MailWorker.ParseFile(lines, tags, conds);
if (parsed)
{
    MailWorker.Send("mrossi@example.com", subject, body);
}
```

---

## Namespaces & Classes

### `OSKHelpers.Mail`

| Class / Interface | Description |
|---|---|
| `MailWorker` | Central static class for all mail operations. Manages SMTP and IMAP connection parameters, parses template files, and sends HTML messages (synchronously or asynchronously) via MailKit. |
| `ISettingsSmtp` | SMTP settings (`SmtpServer`, `SmtpPort`, `SmtpSSL`, `SmtpUsername`, `SmtpPassword`, `SmtpFrom`) of a settings class, accepted by `SetupSmtp(ISettingsSmtp, startTls)` and by `Setup<T>`. |
| `ISettingsImap` | IMAP settings (`ImapServer`, `ImapPort`, `ImapSSL`, `ImapUsername`, `ImapPassword`) of a settings class, accepted by `SetupImap(ISettingsImap)` and by `Setup<T>`. |

#### SMTP configuration

`MailWorker` exposes a set of static properties for SMTP; they are process-wide: the last assignment wins.

| Property | Description |
|---|---|
| `SmtpServer` | Address of the SMTP server. |
| `SmtpPort` | SMTP server port. |
| `SmtpSSL` | Enables implicit SSL/TLS for the SMTP connection (e.g. port 465). |
| `SmtpStartTls` | Upgrades the connection to TLS with STARTTLS (e.g. port 587); used only when `SmtpSSL` is `false`. Default `false`: without `SmtpSSL` and `SmtpStartTls` the connection is not encrypted. |
| `SmtpUsername` / `SmtpPassword` | Credentials for SMTP authentication (authentication is disabled when both are empty). The password is kept obfuscated in memory (see [Passwords](#passwords)). |
| `SmtpFrom` | Optional default sender address used when no `from` is specified in a send call. |
| `SmtpAuthEnabled` | `true` when both username and password are set. |
| `SmtpAuthConfigError` | `true` when only one of the two credentials is set (configuration error). |

Configuration can be loaded from an INI file via `SetupSmtp(IniFileHelper, protector)`, from a settings class via
`SetupSmtp(ISettingsSmtp, startTls, protector)`, from JSON settings via `Setup<T>` (see [JSON settings](#json-settings)), or set programmatically via `SetupSmtp(server, port, ssl, username, password,
from, startTls)`. The optional `protector` decrypts an encrypted password (see [Passwords](#passwords)). The INI key `SmtpStartTls` is optional (default `false`); INI keys are case-insensitive.
`GetSecureSocketOptions(ssl, startTls)` returns the MailKit `SecureSocketOptions` used for the connection.

#### IMAP configuration

| Property | Description |
|---|---|
| `ImapServer` | Address of the IMAP server. |
| `ImapPort` | IMAP server port. |
| `ImapSSL` | Enables SSL/TLS for the IMAP connection. |
| `ImapUsername` / `ImapPassword` | Credentials for IMAP authentication (both required). The password is kept obfuscated in memory. |
| `ImapAuthConfigError` | `true` when username or password is missing. |

Configuration is loaded from an INI file via `SetupImap(IniFileHelper, protector)`, from a settings class via
`SetupImap(ISettingsImap, protector)`, or set programmatically via `SetupImap(server, port, ssl, username, password)`.

> **ToDo:** receiving e-mails via IMAP is not implemented yet: the library only stores and checks the IMAP parameters
> (`CheckImapParameters`).

#### Passwords

- **Encrypted in the configuration**: `SmtpPassword` and `ImapPassword` can be stored encrypted with
  [`CredentialProtector`](https://github.com/ObySamKenoby/OSKHelpers/blob/main/src/OSKHelpers/ReadMe.md) of OSKHelpers
  (`DPAPI:` prefix on Windows, `AES:` / `AESCBC:` with a key file on other systems). Pass the protector to `SetupSmtp` /
  `SetupImap`, created with the same entropy used to encrypt the value. A plain-text value is used as it is. An encrypted value
  that cannot be decrypted (other computer, missing key file, different entropy) or passed without a protector is logged as an
  error and ignored: the password stays empty and `SetupSmtp` / `SetupImap` return `false`.
- **Obfuscated in memory**: the properties are read and written in plain text, but the value is kept as read from the
  configuration (still encrypted, when it was) and obfuscated with `PasswordObfuscator`; each read decodes it and decrypts it
  with the protector received by the setup. Calling `PasswordObfuscator.ReSeed()` makes the stored passwords unreadable: set
  them again afterwards.
- `SetupSmtp` / `SetupImap` never write the configuration: with an INI file, encrypting a plain-text password (e.g. written by
  an operator) is up to the application, which owns the file. JSON settings can use `Setup<T>`, which encrypts and saves them.

```csharp
var iniFile = new IniFileHelper(true);
if (MailWorker.CheckIniPasswordsProtection(iniFile).SmtpRequiresProtection)
{
    // Encrypted on the first start with the default protector; Save keeps the comments and layout of Settings.ini
    // (see OSKHelpers' IniFileHelper)
    var (success, encrypted) = MailWorker.ProtectPassword(iniFile.GetString("SmtpPassword"));
    if (success)
    {
        iniFile.Set("SmtpPassword", encrypted);
        iniFile.Save();
    }
}

var ready = MailWorker.SetupSmtp(iniFile);      // no protector: the default one decrypts the password
```

#### JSON settings

`Setup<T>(settings, filename, protector, smtpStartTls, saveProtectedPasswords)` configures SMTP and IMAP from a
`JsonSettings<T>` class implementing `ISettingsSmtp`, `ISettingsImap` or both, and makes sure the passwords are stored
encrypted:

- a password in plain text is encrypted with `protector` (if null, the default protector, see below) and written back to the settings object, so every later `Save` writes it encrypted;
- the file is then saved with `JsonSettings<T>.Save(filename, null, ignoreBackup: true)`: no backup copy is made, because it
  would keep the password in plain text. Pass `saveProtectedPasswords: false` to skip the save;
- the result is `(SmtpIsValid, ImapIsValid, PlainTextPasswords)`: `SmtpIsValid` / `ImapIsValid` are `false` when the class does
  not implement the interface; `PlainTextPasswords` is `true` when the file still contains a plain-text password (save skipped
  or failed, encryption failed: errors are logged).

**Default protector**: when no protector is passed (to `Setup<T>`, `SetupSmtp` or `SetupImap`), the entropy is read from
`MailWorker.DEFAULTENTROPYFILENAME` (`__mail.entropy`) in `Paths.DefaultConfigsDirectory`, created on first use with 32 random
bytes (readable only by its owner on systems other than Windows). The entropy is kept obfuscated in memory and the default
protector is created only for each encryption or decryption. Deleting or replacing the file makes the passwords encrypted
with it no longer decryptable: write them again in plain text and they will be encrypted with the new entropy.

**INI files**: `CheckIniPasswordsProtection(iniFile)` returns `(SmtpRequiresProtection, ImapRequiresProtection)`, `true` for
a password stored in plain text; the file is not modified. `ProtectPassword(password, protector)` encrypts a password with
`protector` or, if null, with the default protector, and returns `(Success, Value)`; an encrypted or empty password is
returned unchanged.

```csharp
public class AppSettings : JsonSettings<AppSettings>, ISettingsSmtp, ISettingsImap { /* ... */ }

var (settings, _) = AppSettings.Load("Settings.json");
var (smtpReady, imapReady, plainText) = MailWorker.Setup(settings, "Settings.json");
```

#### Sending messages

| Method | Description |
|---|---|
| `SendAsync(...)` / `Send(...)` | Sends an HTML message to one or more recipients with optional stream-based attachments (`((FileName, Content) File, Encoding)`). Overloads without server parameters use the static SMTP settings, overloads without sender use `SmtpFrom`; the full overload accepts the `startTls` parameter (default `false`). Return `true` when the message has been sent; errors are logged with `SimpleLog`. |
| `SendReplyToAsync(...)` / `SendReplyTo(...)` | As above, setting one or more `Reply-To` addresses. |

#### Templates

| Method | Description |
|---|---|
| `ParseFile(lines, tags, conditions, customTags)` | Processes the lines of a template file (a `List<string>`, e.g. `File.ReadAllLines(...).ToList()`): removes comment lines (`#`), extracts the subject from the `@Subject:` line and the optional custom `@Tag:` lines requested in `customTags`, and returns `(Parsed, Subject, Body, TagValues)` ready to be passed to `SendAsync`. Conditions can be `bool` values or `Func<bool>` callbacks. |
| `ParseString(content, tags, conditions)` | Replaces the `{TAG}` placeholders with the provided values and shows or hides the `<<COND>>...<</COND>>` sections according to their conditions (processed before the tags; names are case sensitive). |

#### Diagnostics

| Method | Description |
|---|---|
| `DumpStmpParameters` | Prints or logs the current SMTP settings (password is masked). |
| `DumpImapParameters` | Prints or logs the current IMAP settings (password is masked). |
| `DumpParameters` | Prints or logs both SMTP and IMAP settings. |
| `CheckSmtpParameters` / `CheckImapParameters` | Check that server, port and credentials are present (no connectivity check). |
| `SetLogLevel` | Sets the `SimpleLog` log level for the library. |

---

## Content files

The package adds to the project that installs it, in the `.OSKHelpers.content` folder:

| File | Description |
|---|---|
| `SettingsTemplate.Mail.ini` | Ready-to-use INI template documenting all supported SMTP and IMAP keys: copy it to your project and rename it `Settings.ini`. |
| `MailTemplates/0Index.txt` | Index and usage notes for the template system. |
| `MailTemplates/Credentials.txt` | Example template for credential-notification e-mails (tags, conditions and subject). |

---

## Dependencies

| Package | Purpose |
|---|---|
| [MailKit](https://www.nuget.org/packages/MailKit) | SMTP/IMAP transport layer (version by target, see below). |
| [OSKHelpers](https://www.nuget.org/packages/OSKHelpers) 2.1.0 | Logging (`SimpleLog`), INI configuration (`IniFileHelper`), password protection (`CredentialProtector`, `PasswordObfuscator`). |
| [System.Security.Cryptography.Pkcs](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs) | Cryptography support required by MimeKit 4 (S/MIME); `netstandard2.0` build only. |

### MailKit versions

MailKit 5 turned `SmtpClient.SendAsync(MimeMessage)` into an extension method: an assembly built against MailKit 4 fails at
runtime with MailKit 5 (`MissingMethodException`) and vice versa. The package therefore contains one build for each version:

| Target | MailKit | Used by |
|---|---|---|
| `net8.0` | 5.0.0 or later | Applications targeting .NET 8 or later. |
| `netstandard2.0` | from 4.16.0 to below 5.0.0 | Other applications, e.g. .NET Framework. |

An application on .NET 8 or later that needs to stay on MailKit 4 must keep using OSKHelpers.Mail 1.x.

---

## Changes in 2.0.0

- Requires OSKHelpers 2.1.0 (previously 1.7.5).
- New `net8.0` build against MailKit 5; the `netstandard2.0` build stays on MailKit 4 (`[4.16.0, 5.0.0)`).
- `SetupSmtp` and `SetupImap` (from `IniFileHelper` or from `ISettingsSmtp` / `ISettingsImap`) accept an optional
  `CredentialProtector` to read encrypted passwords.
- `SmtpPassword` and `ImapPassword` are kept obfuscated in memory with `PasswordObfuscator`, still encrypted when they were read
  encrypted, and decrypted at each read.
- Without a protector, a default one is used, whose entropy is read from `__mail.entropy` in `Paths.DefaultConfigsDirectory`
  (created on first use).
- New `CheckIniPasswordsProtection` to check whether the passwords of an INI file are stored in plain text, and
  `ProtectPassword` to encrypt them, also with the default protector.
- New `Setup<T>` for `JsonSettings<T>` classes: encrypts the plain-text passwords and saves the file without a backup copy
  (requires `JsonSettings.Save(..., ignoreBackup)` of OSKHelpers 2.1.0).
- Source compatible with 1.2.5; assemblies compiled against 1.x must be recompiled, because the `Setup*` signatures changed.

---

## License

BSD 3-Clause — see the [License](https://github.com/ObySamKenoby/OSKHelpers/blob/main/LICENSE.txt) file.
