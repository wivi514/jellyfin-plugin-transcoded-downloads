namespace Jellyfin.Plugin.TranscodedDownloads.Enums
{
    /// <summary>
    /// Video codec enumeration.
    /// </summary>
    public enum VideoCodec
    {
        /// <summary>
        /// Copy codec (use original stream).
        /// </summary>
        Copy,

        /// <summary>
        /// H.264 codec.
        /// </summary>
        H264,

        /// <summary>
        /// H.265 codec.
        /// </summary>
        H265,

        /// <summary>
        /// AV1 codec.
        /// </summary>
        Av1,

        /// <summary>
        /// VP9 codec.
        /// </summary>
        Vp9
    }
}
