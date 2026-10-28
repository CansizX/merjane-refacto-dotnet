using Refacto.DotNet.Controllers.Services.Time;

namespace Refacto.Dotnet.Controllers.Tests.Fakes
{
    /// <summary>Clock frozen at a given instant, to make date-based rules deterministic.</summary>
    public class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTime now)
        {
            Now = now;
        }

        public DateTime Now { get; }
    }
}
