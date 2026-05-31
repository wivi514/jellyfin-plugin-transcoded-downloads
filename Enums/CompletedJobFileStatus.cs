namespace Jellyfin.Plugin.TranscodedDownloads.Enums
{
    /// <summary>
    /// Status for retrieving a completed job file.
    /// </summary>
    public enum CompletedJobFileStatus
    {
        /// <summary>
        /// The job does not exist.
        /// </summary>
        NotFound,

        /// <summary>
        /// The job exists but is not completed.
        /// </summary>
        NotCompleted,

        /// <summary>
        /// The job is completed, but the file no longer exists.
        /// </summary>
        FileMissing,

        /// <summary>
        /// The completed output file is available.
        /// </summary>
        Available
    }
}
