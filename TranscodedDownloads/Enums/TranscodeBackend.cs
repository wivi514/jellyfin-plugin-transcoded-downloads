namespace Jellyfin.Plugin.TranscodedDownloads.Enums
{
    /// <summary>
    /// Transcoding backend enumeration.
    /// </summary>
    public enum TranscodeBackend
    {
        /// <summary>
        /// Software-based transcoding.
        /// </summary>
        Software,

        /// <summary>
        /// VA-API (Video Acceleration API) based transcoding.
        /// </summary>
        Vaapi,

        /// <summary>
        /// Intel Quick Sync Video transcoding.
        /// </summary>
        Qsv,

        /// <summary>
        /// NVIDIA NVENC transcoding.
        /// </summary>
        Nvenc,

        /// <summary>
        /// AMD Media Framework transcoding.
        /// </summary>
        Amf,

        /// <summary>
        /// Apple VideoToolbox transcoding.
        /// </summary>
        VideoToolbox,

        /// <summary>
        /// Rockchip Media Processing Platform transcoding.
        /// </summary>
        Rkmpp
    }
}
