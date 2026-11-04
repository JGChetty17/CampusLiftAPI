using System;
namespace CampusLift.API.Extensions
{
    /// <summary>
    /// Shared DateTime helpers. Extracted from TripsController so other controllers
    /// (Messages, Bookings) can normalise timestamps consistently.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Normalises a DateTime to UTC. The Supabase/Newtonsoft read path drops
        /// the offset from timestamptz values and returns wall-clock DateTime with
        /// Kind=Unspecified. Postgres stores timestamptz in UTC, so an Unspecified
        /// value from that path is already UTC wall-clock — we just tag it.
        ///
        /// Reference: https://learn.microsoft.com/dotnet/api/system.datetime.specifykind
        /// </summary>
        public static DateTime AsUtc(this DateTime dt) => dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };
    }
}