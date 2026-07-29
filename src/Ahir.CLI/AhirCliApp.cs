namespace Ahir.CLI;

public sealed class AhirCliApp
{
    private readonly Func<Task> _start;
    private readonly Func<Task> _stop;
    private readonly Func<Task> _restart;
    private readonly Action _status;
    private readonly Func<Task> _backup;
    private readonly Func<string, Task> _restore;
    private readonly Action _logs;
    private readonly Action _config;
    private readonly Action _doctor;

    public AhirCliApp(
        Func<Task> start, Func<Task> stop, Func<Task> restart, Action status,
        Func<Task> backup, Func<string, Task> restore, Action logs, Action config, Action doctor)
    {
        _start = start;
        _stop = stop;
        _restart = restart;
        _status = status;
        _backup = backup;
        _restore = restore;
        _logs = logs;
        _config = config;
        _doctor = doctor;
    }

    public async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var commandArgs = args[1..];

        try
        {
            return command switch
            {
                "start" => await ExecuteAsync(_start),
                "stop" => await ExecuteAsync(_stop),
                "restart" => await ExecuteAsync(_restart),
                "status" => ExecuteSync(_status),
                "backup" => await ExecuteAsync(_backup),
                "restore" when commandArgs.Length > 0 => await ExecuteAsync(() => _restore(commandArgs[0])),
                "logs" => ExecuteSync(_logs),
                "config" => ExecuteSync(_config),
                "doctor" => ExecuteSync(_doctor),
                "help" => PrintHelpAndReturn(),
                _ => PrintHelpAndReturn()
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ExecuteAsync(Func<Task> action)
    {
        await action();
        return 0;
    }

    private static int ExecuteSync(Action action)
    {
        action();
        return 0;
    }

    private static int PrintHelpAndReturn()
    {
        PrintHelp();
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Ahir - Next-generation backend platform");
        Console.WriteLine();
        Console.WriteLine("Usage: ahir <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  start     Start the Ahir server");
        Console.WriteLine("  stop      Stop the Ahir server");
        Console.WriteLine("  restart   Restart the Ahir server");
        Console.WriteLine("  status    Show server status");
        Console.WriteLine("  backup    Create a backup");
        Console.WriteLine("  restore   Restore from a backup");
        Console.WriteLine("  logs      View server logs");
        Console.WriteLine("  config    Manage configuration");
        Console.WriteLine("  doctor    Run system diagnostics");
        Console.WriteLine("  help      Show this help");
    }
}