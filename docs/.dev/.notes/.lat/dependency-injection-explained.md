### Example Scenario

To keep things consistent and relatable to your Discord bot project, let's implement a simple "LogUserAction"
feature: It takes a username and action (e.g., "joined guild"), logs it with a timestamp, and handles two
backends—file logging or database logging (simulated with Npgsql placeholder). We'll show *all* versions doing
*exactly* the same thing: Log to file by default, but swappable to DB.

I'll group them by style, with runnable C# snippets (tested mentally—copy-paste into a console app). Each
includes a `Main` to demo usage. Focus on the DI (or lack thereof) in the core logic. I've added explanatory
comments (using `//`) to highlight key concepts, especially how dependencies are handled (or not).

### 1. Without DI (Hardcoded Dependencies)
These versions "bake in" the logging mechanism directly, making swaps hard (e.g.,
 no easy testing without real files/DB).

#### Non-DI OOP (Class with Internal Creation)
Classes create their own logger—tightly coupled, classic OOP anti-pattern.
```csharp
using System;
using System.IO;

// Interface for logging (but not used for injection here—it's OOP without DI)
public interface ILogger
{
    void Log(string message);
}

public class FileLogger : ILogger  // Concrete implementation for file logging
{
    private readonly string _filePath;

    public FileLogger(string filePath)
    {
        _filePath = filePath;  // Constructor sets up the file path
    }

    public void Log(string message)
    {
        File.AppendAllText(_filePath, message + "\n");  // Writes directly to file—hardcoded behavior
    }
}

public class UserLogger
{
    private readonly FileLogger _logger;  // Dependency is created internally—tight coupling!

    public UserLogger()
    {
        _logger = new FileLogger("logs.txt");  // Hardcoded: Always creates a FileLogger instance here
        // No way to inject a different logger (e.g., DbLogger) without changing this constructor
    }

    public void LogUserAction(string username, string action)
    {
        var message = $"{DateTime.Now}: {username} did {action}";  // Builds the log message
        _logger.Log(message);  // Uses the hardcoded logger—swapping requires code changes
    }
}

class Program
{
    static void Main()
    {
        var logger = new UserLogger();  // Creates the class with its internal hardcoded dependency
        logger.LogUserAction("alice", "joined guild");  // Runs, but can't easily test or swap logger
    }
}
```
**Issues**: To use DB, rewrite `UserLogger` constructor or add a factory—messy for big apps.

#### Non-DI Procedural (Static Function with Internal Creation)
Flat script style: Function creates its own tool inside—no classes.
```csharp
using System;
using System.IO;

static void LogUserAction(string username, string action)
{
    // Hardcoded dependency creation: Always uses file logging directly inside the function
    var filePath = "logs.txt";  // Fixed path—no flexibility
    var message = $"{DateTime.Now}: {username} did {action}";  // Builds the log message
    File.AppendAllText(filePath, message + "\n");  // Directly calls file API—equivalent to creating a logger on the fly
    // To swap to DB (e.g., Npgsql insert), you'd edit this function body—procedural but inflexible
}

class Program
{
    static void Main()
    {
        LogUserAction("alice", "joined guild");  // Calls the function—simple, but no injection for testing/swaps
    }
}
```
**Issues**: Even simpler than OOP, but still inflexible—like old Python with hardcoded paths.

#### Non-DI Functional (Higher-Order Functions, But Hardcoded)
C# functional-ish: Use lambdas, but the "logger" lambda is defined and called internally—no injection.
```csharp
using System;
using System.IO;

// Factory function that returns a logger lambda—still hardcoded to file logic
static Action<string> CreateFileLogger() => message =>
{
    // Internal hardcoded behavior: Always appends to a fixed file
    File.AppendAllText("logs.txt", message + "\n");  // No params for path or type—tightly coupled
};

static void LogUserAction(string username, string action)
{
    var logger = CreateFileLogger();  // Creates the logger inside—no argument to inject a different one
    // If you wanted DB logging, you'd redefine CreateFileLogger entirely
    var message = $"{DateTime.Now}: {username} did {action}";  // Builds the message functionally (immutable)
    logger(message);  // Invokes the hardcoded lambda
}

class Program
{
    static void Main()
    {
        LogUserAction("alice", "joined guild");  // Pure function call, but logger is baked in
    }
}
```
**Issues**: Feels pure (immutable, no state), but still couples the function to file logging. Swapping means redefining `CreateFileLogger`.

### 2. OOP DI (Constructor Injection)
Standard OOP: Class declares a dependency interface, injects via constructor. Uses a DI container for wiring (Microsoft's built-in).
```csharp
using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;  // Built-in .NET DI container for automatic wiring

public interface ILogger  // Abstraction: Defines contract for any logger—key to DI flexibility
{
    void Log(string message);
}

public class FileLogger : ILogger  // One implementation: File-based
{
    private readonly string _filePath;
    public FileLogger(string filePath) => _filePath = filePath;  // Can be configured externally
    public void Log(string message) => File.AppendAllText(_filePath, message + "\n");  // Simple file write
}

public class DbLogger : ILogger  // Alternative implementation: DB-based (e.g., for Npgsql)
{
    public void Log(string message)
    {
        // Placeholder for Npgsql: In real code, this would insert into your repo table
        Console.WriteLine($"[DB] {message}");  // Simulates DB logging
    }
}

public class UserLogger  // Main class: Depends on ILogger, but doesn't create it
{
    private readonly ILogger _logger;  // Dependency field—will be injected

    public UserLogger(ILogger logger)  // Constructor injection: Receives the dependency from outside
    {
        _logger = logger;  // Stores the injected instance—could be FileLogger or DbLogger
        // No hardcoded creation here—that's the point of DI!
    }

    public void LogUserAction(string username, string action)
    {
        var message = $"{DateTime.Now}: {username} did {action}";  // Business logic unchanged
        _logger.Log(message);  // Uses whatever was injected—decoupled from specific impl
    }
}

class Program
{
    static void Main()
    {
        // DI Container setup: Registers ILogger as FileLogger (easy to swap to DbLogger)
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, FileLogger>();  // Singleton lifetime; configure path via AddSingleton(new FileLogger("path"))
        // For DB: services.AddSingleton<ILogger, DbLogger>();
        var provider = services.BuildServiceProvider();  // Builds the "wiring" graph

        var logger = provider.GetRequiredService<UserLogger>();  // Resolves UserLogger with injected deps
        logger.LogUserAction("alice", "joined guild");  // Works with injected logger—testable by mocking ILogger
    }
}
```
**Benefits**: Flexible—change logger in one spot (DI container). Great for complex apps, but more setup.

### 3. Procedural DI (Function Arguments with Delegates)
Your Python-like sweet spot: Pass dependencies as params (delegates). No classes, just functions composing.
```csharp
using System;
using System.IO;

// Delegate type: Defines a "contract" for logging functions—like a Python func pointer
// Any static func matching this signature can be passed as a dependency
delegate void Logger(string message);

static void LogUserAction(Logger logger, string username, string action)
{
    // No internal creation: Relies entirely on the injected logger param
    // This decouples the function—logger could be file, DB, or a mock for tests
    var message = $"{DateTime.Now}: {username} did {action}";  // Core logic
    logger(message);  // Calls the injected function—procedural DI in action
}

// Static function implementations: These are the "providers" of logging behavior
static void FileLogger(string message) 
{ 
    File.AppendAllText("logs.txt", message + "\n");  // File-specific impl—passed as arg when needed
}

static void DbLogger(string message)
{
    // Npgsql placeholder: In full code, this would use your repo connection
    Console.WriteLine($"[DB] {message}");  // Simulates DB insert
}

class Program
{
    static void Main()
    {
        // Manual injection: Assign static funcs to the delegate and pass as arg
        // Super flexible—swap per call without editing LogUserAction
        Logger fileLog = FileLogger;  // "Injects" file logging
        LogUserAction(fileLog, "alice", "joined guild");

        Logger dbLog = DbLogger;  // Swap to DB logging
        LogUserAction(dbLog, "bob", "updated profile");  // Same function, different dep
    }
}
```
**Benefits**: Dead simple, testable (pass mocks), scales procedurally. For deeper stacks, pass a "context" with all deps.

These all output the same log line but highlight trade-offs: Non-DI is quick for tiny scripts but brittle; OOP DI shines for structured apps; procedural DI bridges your Python roots with C#'s power. If you want to extend this (e.g., add async for Npgsql), just say!