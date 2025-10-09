# Cybrancee

| File                                    | Keep?      | Commit? | Purpose                                                        |
|-----------------------------------------|------------|---------|----------------------------------------------------------------|
| `appsettings.json`                      | ✅ Yes      | ✅ Yes   | Base defaults shared across all environments. No secrets.      |
| `appsettings.Development.json`          | Optional   | 🚫 No   | Your local dev settings (can have local DB or tokens).         |
| `appsettings.Development.json.template` | ✅ Yes      | ✅ Yes   | Template showing what keys devs should fill in.                |
| `appsettings.Production.json`           | ❌ Optional | 🚫 No   | Usually replaced by `.env` on Cybrancee, so you don’t need it. |