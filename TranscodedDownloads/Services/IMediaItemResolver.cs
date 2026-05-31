using System;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Resolves Jellyfin item IDs to source media information.
    /// </summary>
    public interface IMediaItemResolver
    {
        /// <summary>
        /// Resolves a Jellyfin item ID.
        /// </summary>
        /// <param name="itemId">The Jellyfin item ID.</param>
        /// <returns>The resolved media item info.</returns>
        MediaItemInfo ResolveItem(Guid itemId);
    }
}
