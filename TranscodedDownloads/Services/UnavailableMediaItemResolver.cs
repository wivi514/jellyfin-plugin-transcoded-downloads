using System;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Fallback resolver used until Jellyfin dependency injection wires a real resolver.
    /// </summary>
    internal sealed class UnavailableMediaItemResolver : IMediaItemResolver
    {
        /// <inheritdoc />
        public MediaItemInfo ResolveItem(Guid itemId)
        {
            throw new MediaItemResolutionException("Media item resolution is not configured.");
        }
    }
}
