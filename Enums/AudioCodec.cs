namespace Jellyfin.Plugin.TranscodedDownloads.Enums
{
    /// <summary>
    /// Audio codec enumeration.
    /// </summary>
    public enum AudioCodec
    {
        /// <summary>
        /// Copy codec (use original stream).
        /// </summary>
        Copy,

        /// <summary>
        /// AAC codec.
        /// </summary>
        Aac,

        /// <summary>
        /// MP3 codec.
        /// </summary>
        Mp3,

        /// <summary>
        /// Opus codec.
        /// </summary>
        Opus,

        /// <summary>
        /// FLAC codec.
        /// </summary>
        Flac
    }
}
```