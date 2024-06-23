using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    /// <summary>
    /// A status message that will be aggregated in a single message
    /// </summary>
    public interface IStatusUpdate
    {
        /// <summary>
        /// Update a message.
        /// </summary>
        bool Append(string text);
    }

    /// <summary>
    /// Service for notifications (e.g. emails...)
    /// </summary>
    public interface INotificationService : IService
    {
        /// <summary>
        /// Send an immediate text mail (not coalesced)
        /// </summary>
        Task SendMail(string title, string body, bool isAdminReport);

        /// <summary>
        /// Enqueue a low-priority notification (sent aggregated on throttle basis).
        /// Returns an object to modify the status update, if the update was not yet send.
        /// </summary>
        IStatusUpdate EnqueueStatusUpdate(string groupTitle, string message);
    }
}
