# WabbitBot Configuration Guide

## Overview

WabbitBot uses the modern .NET configuration system with `IConfiguration` and the Options pattern. This provides a 
flexible, secure, and maintainable way to configure the bot.

## Configuration Files

### Primary Configuration

- **`appsettings.json`** - Main configuration file with all bot settings
- **`appsettings.Development.json`** - Development-specific overrides

### Environment Variables

* **Local Development:** uses `dotnet user-secrets`
* **Hosting on Cybrancee:** uses a `.env` file (loaded with `DotNetEnv`)

```markdown
## 🔐 Configuring Secrets & Environments (Local + Cybrancee)

WabbitBot uses [ASP.NET Core configuration binding](https://learn.microsoft.com/aspnet/core/fundamentals/configuration) 
to manage sensitive settings securely. During local development, secrets are stored safely using **User Secrets**, 
and in deployment (on Cybrancee), they are loaded from a **`.env` file**.

Configuration order:
1. `appsettings.json` — base defaults  
2. `appsettings.{Environment}.json` — optional overrides  
3. **User Secrets** (local development only)  
4. **.env file (Cybrancee hosting)** — environment-based secrets  

Only **`WabbitBot.Host`** needs secrets.  

---

### 🧱 1. Initialize User Secrets (Local Development)

Run these commands once in the host project:

```bash
cd src/WabbitBot.Host
dotnet user-secrets init
````

This adds a `<UserSecretsId>` entry to your `.csproj` file:

```xml
<PropertyGroup>
  <UserSecretsId>3c5e2e13-18ab-4a42-851b-9bfc70b83f73</UserSecretsId>
</PropertyGroup>
```

---

### ⚙️ 2. Set Secrets for Local Development

Set your local Discord token and database connection:

```bash
dotnet user-secrets set "Bot:Token" "your-local-discord-token" --project src/WabbitBot.Host/WabbitBot.Host.csproj

dotnet user-secrets set "Bot:Database:ConnectionString" "Host=localhost;Database=wabbitbot;Username=wabbitbot;Password=devpw" --project src/WabbitBot.Host/WabbitBot.Host.csproj

dotnet user-secrets set "ASPNETCORE_ENVIRONMENT" "Development" --project src/WabbitBot.Host/WabbitBot.Host.csproj
```

List current secrets:

```bash
dotnet user-secrets list --project src/WabbitBot.Host/WabbitBot.Host.csproj
```

These secrets are stored securely in:

* **Windows:** `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
* **Linux/macOS:** `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`

Run the app locally:

```bash
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project src/WabbitBot.Host
```

ASP.NET Core automatically merges the `appsettings.*.json` files with your local user secrets.

---

### 🌐 3. Deploying on Cybrancee

Cybrancee uses a `.env` file to store environment variables for your bot. This file should be placed in your root 
project folder (`/src/WabbitBot.Host`) when uploading via SFTP or created in the Cybrancee panel.

**Create a file named `.env`** with the following content:

```
ASPNETCORE_ENVIRONMENT=Development
Bot__Token=your-discord-bot-token
Bot__Database__ConnectionString=Host=localhost;Database=wabbitbot;Username=wabbitbot;Password=serverpw;
```

> **Note:** The double underscore `__` maps to nested JSON keys in ASP.NET Core (e.g., `Bot:Token`).

---

### 🧩 4. Loading the `.env` File in Code

In your `Program.cs`, ensure this line appears near the top **before** the configuration builder:

```csharp
using DotNetEnv;

Env.Load(); // Load variables from .env into the environment
```

Then ASP.NET Core automatically reads these variables during startup.

---

### 🧱 5. Starting the Bot on Cybrancee

1. Open your **Cybrancee dashboard**.
2. Select your **C# (Discord Bot)** server.
3. Upload your published files (from `dotnet publish -c Release -o out/`).
4. Upload the `.env` file into the same directory.
5. Set the **Startup Command** to:

   ```
   dotnet WabbitBot.Host.dll
   ```
6. Start the server.
   The `.env` file will load automatically, and the bot will connect using your `Bot__Token`.

---

### 🧹 6. Rotating or Clearing Local Secrets

If you ever need to clear local secrets:

```bash
dotnet user-secrets remove "Bot:Token" --project src/WabbitBot.Host/WabbitBot.Host.csproj
dotnet user-secrets clear --project src/WabbitBot.Host/WabbitBot.Host.csproj
```

---

### ✅ Summary

| Environment           | Secret Storage        | Example File/Command                        | Automatically Loaded |
|-----------------------|-----------------------|---------------------------------------------|----------------------|
| **Local Development** | `dotnet user-secrets` | `dotnet user-secrets set "Bot:Token" "..."` | ✅ Yes                |
| **Cybrancee Hosting** | `.env` file           | `Bot__Token=...` in `.env`                  | ✅ With `Env.Load()`  |

---

### 🔒 Security Notes

* Never commit `.env` or secrets to Git.
* Reset your Discord token immediately if it’s ever exposed.
* Keep `.env` file permissions restricted (read-only for your user).
* User Secrets are encrypted per user and safe for local development.

```

---

✅ This version removes all references to “Production,”  
assumes **Cybrancee** is your deployment platform,  
and uses the **exact file names and structure** from your repo (`src/WabbitBot.Host/...`).
```


## Configuration Structure

The configuration is organized into logical sections:

```json
{
  "Bot": {
    "LogLevel": "Information",
    "ServerId": null,
    "Database": { ... },
    "Channels": { ... },
    "Roles": { ... },
    "Activity": { ... },
    "Scrimmage": { ... },
    "Tournament": { ... },
    "Match": { ... },
    "Leaderboard": { ... }
  }
}
```

## Environment Variable Overrides

You can override any configuration value using environment variables:

```bash
# Override specific settings
WABBITBOT_LEADERBOARD_DISPLAYTOPN=20
```

## Configuration Validation

The system automatically validates configuration on startup:
- Required fields are present
- Numeric values are within valid ranges
- Logical constraints are satisfied

## Security Best Practices

1. **Never commit sensitive data** to version control
2. **Use environment variables** for tokens and API keys
3. **Use different configs** for different environments
4. **Restrict file permissions** on configuration files

## Troubleshooting

### Common Issues

**Configuration not loading:**
- Check file paths and permissions
- Verify JSON syntax is valid
- Ensure required fields are present

**Environment variables not working:**
- Check variable names (case-sensitive)
- Verify environment is set correctly
- Restart application after changes

**Validation errors:**
- Check numeric ranges
- Verify required fields
- Review logical constraints
