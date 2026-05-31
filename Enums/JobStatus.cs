using System;

namespace Jellyfin.Plugin.TranscodedDownloads.Enums
{
    /// <summary>
    /// Status of a download job.
    /// </summary>
    public enum JobStatus
    {
        /// <summary>
        /// Job is queued for processing.
        /// </summary>
        Queued,

        /// <summary>
        /// Job is currently running.
        /// </summary>
        Running,

        /// <summary>
        /// Job has completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Job has failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Job has been cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Job has expired.
        /// </summary>
        Expired
    }
}
