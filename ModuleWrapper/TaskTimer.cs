namespace ModuleWrapper
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    public class TaskTimer
    {
        CancellationTokenSource cancellationTokenSource;
        TimeSpan timerPeriod;
        Action onElapsedCallback;
        bool continueOnError;

        public TaskTimer(Action onElapsedCallback,
            TimeSpan timerPeriod,
            CancellationTokenSource cancellationTokenSource,
            bool continueOnError = true)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            this.timerPeriod = timerPeriod;
            this.onElapsedCallback = onElapsedCallback;
            this.continueOnError = continueOnError;
        }

        public void Start()
        {
            Task elapsedTask = null;
            elapsedTask = new Task((x) =>
            {
                Elapsed(elapsedTask, cancellationTokenSource);
            }, cancellationTokenSource.Token);

            HandleError(elapsedTask);

            elapsedTask.Start();
        }

        private void Elapsed(Task task, object objParam)
        {
            var start = DateTime.Now;
            var cancellationTokenSource = (CancellationTokenSource)objParam;
            if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                Log.Information("A cancellation has been requested.");
                return;
            }

            onElapsedCallback();

            var delay = timerPeriod - (DateTime.Now - start);
            if (delay.Ticks > 0)
            {
                Log.Verbose($"Waiting for {delay}");
                task = Task.Delay(delay);
            }
            HandleError(task.ContinueWith(Elapsed, cancellationTokenSource));
        }

        private void HandleError(Task task)
        {
            task.ContinueWith((e) =>
             {
                 Log.Error($"Exception when running timer callback: {e.Exception}");
                 if (!continueOnError)
                     cancellationTokenSource.Cancel();
                 else
                     task.ContinueWith(Elapsed, cancellationTokenSource);
             }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}