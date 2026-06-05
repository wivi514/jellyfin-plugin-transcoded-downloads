using System;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;
using Jellyfin.Plugin.TranscodedDownloads.Models;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Resolves Jellyfin library items to source media information.
    /// </summary>
    public sealed class MediaItemResolver : IMediaItemResolver
    {
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaItemResolver"/> class.
        /// </summary>
        /// <param name="libraryManager">The Jellyfin library manager.</param>
        public MediaItemResolver(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager ?? throw new ArgumentNullException(nameof(libraryManager));
        }

        /// <inheritdoc />
        public MediaItemInfo ResolveItem(Guid itemId)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentException("Item ID cannot be empty.", nameof(itemId));
            }

            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                throw new MediaItemResolutionException("The requested media item does not exist.");
            }

            if (string.IsNullOrWhiteSpace(item.Path))
            {
                throw new MediaItemResolutionException("The requested media item does not have a source path.");
            }

            return new MediaItemInfo
            {
                ItemId = item.Id,
                Name = string.IsNullOrWhiteSpace(item.Name) ? item.Id.ToString("N") : item.Name,
                Path = item.Path,
                IsVideo = item.MediaType == Jellyfin.Data.Enums.MediaType.Video,
                IsAudio = item.MediaType == Jellyfin.Data.Enums.MediaType.Audio
            };
        }
    }
}
