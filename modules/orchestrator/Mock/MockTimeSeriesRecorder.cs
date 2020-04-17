
namespace Orchestrator.Mock
{
    using Orchestrator.Abstraction;
    using Serilog;
    using System.Threading.Tasks;

    public class MockTimeSeriesRecorder : ITimeSeriesRecorder
    {
        public Task InitializeAsync()
        {
            return Task.FromResult(0);
        }

        public Task RecordMessageAsync(string telemetryJson)
        {
            Log.Information("Recording telemetry..");
            return Task.FromResult(0);
        }
    }
}