namespace Refacto.DotNet.Controllers.Services.Time
{
    public class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
