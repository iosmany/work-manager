using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Workmanager
{
    sealed class WorkManager : IDisposable, IAsyncDisposable
    {
        CancellationToken cancellationToken;
        readonly List<Task> _tasks;
        readonly Channel<Func<Task>> _channel;
        readonly ConcurrentBag<Exception> _exceptions = new ConcurrentBag<Exception>();

        public WorkManager(int concurrency = 4) : this(concurrency, CancellationToken.None)
        {
        }

        public WorkManager(int concurrency, CancellationToken cancellationToken)
        {
            if (concurrency <= 0)
                throw new ArgumentException("Should be greater than zero.");

            _channel = Channel.CreateUnbounded<Func<Task>>();
            this.cancellationToken = cancellationToken;
            _tasks = new List<Task>(concurrency);
            for (var t = 0; t < concurrency; t++)
                _tasks.Add(ExecuteAsync());
        }

        bool closed;
        public async Task CompletoAsync()
        {
            if (closed)
                return;
            _channel.Writer.Complete(); //close the channel, means no more work are allowed
            closed = true;
            await Task.WhenAll(_tasks);
            if (_exceptions.Any())
                throw new AggregateException(_exceptions);
        }

        public ValueTask OpenWorkAsync(Func<Task> trabajo)
        {
            if (closed)
                throw new Exception("No more work, the channel is closed.");

            return _channel.Writer.WriteAsync(trabajo);
        }

        async Task ExecuteAsync()
        {
            await foreach (var currentProcess in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await currentProcess();
                }
                catch (Exception ex)
                {
                    _exceptions.Add(ex);
                }
            }
        }

        bool disposed = false;
        public void Dispose()
        {
            if (disposed)
                return;
            DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("Disposing...");
            if (disposed)
                return;
            await CompletoAsync();
            disposed = true;
        }
    }
}
