using System;
using MediaBrowser.Common.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.TranscodedDownloads
{
    /// <summary>
    /// The main plugin class for the Transcoded Downloads plugin.
    /// </summary>
    public sealed class Plugin : BasePlugin<PluginConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => "Transcoded Downloads";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("2dff9f1e-7a24-4c58-a1c8-74f4fd5312c8");

        /// <inheritdoc />
        public override string Description => "Download transcoded copies of movies, episodes, and music items directly from Jellyfin.";

    }
}
