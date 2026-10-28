namespace Refacto.DotNet.Controllers.Services.Time
{
    /// <summary>
    /// Abstraction over the system clock so that date-based business rules can be tested.
    /// </summary>
    public interface IDateTimeProvider
    {
        DateTime Now { get; }
    }
}
