// See https://aka.ms/new-console-template for more information
using Workmanager;

Console.WriteLine("Hello, WorkManager!");

static async Task DoWork(int number) //in is a readonly reference
{
    var rnd = new Random();
    Console.WriteLine($"Working {number}...");
    await Task.Delay(rnd.Next(100, 1000));
}

var tasks = new List<Func<Task>>();
Enumerable.Range(0, 10)
    .Select(x =>
    {
        tasks.Add(() => DoWork(x));
        return x;
    }).ToList();

//getting the cpu number to determine the concurrency
var cpUs = Environment.ProcessorCount;
var concurrency = cpUs * 2;

Console.WriteLine($"Cpus {cpUs}");
Console.WriteLine($"Concurrency: {concurrency}");   
Console.WriteLine($"Tasks: {tasks.Count}");

Console.WriteLine("Press any key to start..."); 
Console.ReadKey();
Console.WriteLine("Starting work...");

async static Task ExecuteWork(int concurrency, IEnumerable<Func<Task>> tasks)
{
    using (var wm = new WorkManager(concurrency))
    {
        foreach (var func in tasks)
            await wm.OpenWorkAsync(func);
    }
}

ExecuteWork(concurrency, tasks)
    .ConfigureAwait(false)
    .GetAwaiter()
    .GetResult();


Console.WriteLine("Work done!");
Console.ReadKey();

