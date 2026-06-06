(function () {
    'use strict';

    var pluginId = '2dff9f1e-7a24-4c58-a1c8-74f4fd5312c8';
    var videoCodecs = ['Copy', 'H264', 'H265', 'Av1', 'Vp9'];
    var audioCodecs = ['Copy', 'Aac', 'Mp3', 'Opus', 'Flac'];
    var containers = ['Mp4', 'Mkv', 'Webm', 'Mp3', 'M4a', 'Ogg', 'Flac'];
    var backends = ['Software', 'Vaapi', 'Qsv', 'Nvenc', 'Amf', 'VideoToolbox', 'Rkmpp'];
    var enumNames = {
        video: videoCodecs,
        audio: audioCodecs,
        container: containers,
        backend: backends
    };
    var state = null;

    function qs(page, selector) {
        return page.querySelector(selector);
    }

    function setStatus(page, message) {
        qs(page, '#td-status').textContent = message || '';
    }

    function id() {
        return 'td-' + Math.random().toString(16).slice(2) + Date.now().toString(16);
    }

    function enumName(kind, value) {
        var values = enumNames[kind];
        if (typeof value === 'number') {
            return values[value] || values[0];
        }

        if (typeof value === 'string') {
            var match = values.filter(function (candidate) {
                return candidate.toLowerCase() === value.toLowerCase();
            })[0];
            return match || values[0];
        }

        return values[0];
    }

    function positiveInt(value, fallback) {
        var parsed = parseInt(value, 10);
        return parsed > 0 ? parsed : fallback;
    }

    function optionalPositiveInt(value) {
        if (value === null || value === undefined || value === '') {
            return null;
        }

        var parsed = parseInt(value, 10);
        return parsed > 0 ? parsed : null;
    }

    function ensureArray(value) {
        return Array.isArray(value) ? value : [];
    }

    function defaultProfile() {
        return {
            Id: 'cpu-h264-aac',
            Name: 'CPU H.264',
            Backend: 'Software',
            AllowedVideoCodecs: ['H264'],
            AllowedAudioCodecs: ['Aac'],
            AllowedContainers: ['Mp4'],
            SupportsHardwareDecode: false,
            SupportsHardwareEncode: false,
            SupportsToneMapping: true,
            SupportsSubtitleBurnIn: true,
            SupportsTwoPass: false,
            DevicePath: '',
            Notes: ''
        };
    }

    function defaultPreset(profileId) {
        return {
            Id: '1080p-h264-aac-mp4',
            Name: '1080p H.264 AAC MP4',
            Enabled: true,
            CapabilityProfileId: profileId,
            Container: 'Mp4',
            VideoCodec: 'H264',
            AudioCodec: 'Aac',
            MaxWidth: 1920,
            MaxHeight: 1080,
            VideoBitrateKbps: 8000,
            AudioBitrateKbps: 192,
            AudioChannels: 2,
            AllowStreamCopyWhenCompatible: true,
            BurnSubtitles: false,
            ToneMapHdrToSdr: true,
            PreserveOriginalAudioIfCompatible: false,
            IsVideoPreset: true,
            IsAudioOnlyPreset: false
        };
    }

    function addedDefaultPreset(profileId) {
        var preset = defaultPreset(profileId);
        preset.Id = id();
        return preset;
    }

    function normalizeProfile(profile) {
        profile = profile || {};
        return {
            Id: profile.Id || id(),
            Name: profile.Name || 'CPU H.264',
            Backend: enumName('backend', profile.Backend),
            AllowedVideoCodecs: ensureArray(profile.AllowedVideoCodecs).map(function (value) {
                return enumName('video', value);
            }),
            AllowedAudioCodecs: ensureArray(profile.AllowedAudioCodecs).map(function (value) {
                return enumName('audio', value);
            }),
            AllowedContainers: ensureArray(profile.AllowedContainers).map(function (value) {
                return enumName('container', value);
            }),
            SupportsHardwareDecode: !!profile.SupportsHardwareDecode,
            SupportsHardwareEncode: !!profile.SupportsHardwareEncode,
            SupportsToneMapping: !!profile.SupportsToneMapping,
            SupportsSubtitleBurnIn: !!profile.SupportsSubtitleBurnIn,
            SupportsTwoPass: !!profile.SupportsTwoPass,
            DevicePath: profile.DevicePath || '',
            Notes: profile.Notes || ''
        };
    }

    function normalizePreset(preset, profileId) {
        preset = preset || {};
        var isAudioOnly = !!preset.IsAudioOnlyPreset;
        return {
            Id: preset.Id || id(),
            Name: preset.Name || '1080p H.264 AAC MP4',
            Enabled: preset.Enabled !== false,
            CapabilityProfileId: preset.CapabilityProfileId || profileId,
            Container: enumName('container', preset.Container),
            VideoCodec: isAudioOnly ? 'Copy' : enumName('video', preset.VideoCodec),
            AudioCodec: enumName('audio', preset.AudioCodec),
            MaxWidth: isAudioOnly ? null : preset.MaxWidth,
            MaxHeight: isAudioOnly ? null : preset.MaxHeight,
            VideoBitrateKbps: isAudioOnly ? null : preset.VideoBitrateKbps,
            AudioBitrateKbps: preset.AudioBitrateKbps,
            AudioChannels: preset.AudioChannels,
            AllowStreamCopyWhenCompatible: preset.AllowStreamCopyWhenCompatible !== false,
            BurnSubtitles: !isAudioOnly && !!preset.BurnSubtitles,
            ToneMapHdrToSdr: !isAudioOnly && preset.ToneMapHdrToSdr !== false,
            PreserveOriginalAudioIfCompatible: !!preset.PreserveOriginalAudioIfCompatible,
            IsVideoPreset: !isAudioOnly,
            IsAudioOnlyPreset: isAudioOnly
        };
    }

    function normalizeConfig(config) {
        config = config || {};
        var profiles = ensureArray(config.CapabilityProfiles).map(normalizeProfile);
        if (!profiles.length) {
            profiles.push(defaultProfile());
        }

        var presets = ensureArray(config.Presets).map(function (preset) {
            return normalizePreset(preset, profiles[0].Id);
        });
        if (!presets.length) {
            presets.push(defaultPreset(profiles[0].Id));
        }

        config.EnableVideoDownloads = config.EnableVideoDownloads !== false;
        config.EnableMusicDownloads = config.EnableMusicDownloads !== false;
        config.EnableWebUiInjection = !!config.EnableWebUiInjection;
        config.EnableAdvancedUnsafeFfmpegArguments = !!config.EnableAdvancedUnsafeFfmpegArguments;
        config.MaxConcurrentJobs = positiveInt(config.MaxConcurrentJobs, 1);
        config.MaxQueueSize = positiveInt(config.MaxQueueSize, 10);
        config.JobRetentionHours = positiveInt(config.JobRetentionHours, 24);
        config.MaxTempFolderSizeMb = positiveInt(config.MaxTempFolderSizeMb, 50000);
        config.TempDirectory = config.TempDirectory || '';
        config.CapabilityProfiles = profiles;
        config.Presets = presets;
        config.UserPreferences = ensureArray(config.UserPreferences);
        return config;
    }

    function option(value, label, selected) {
        var element = document.createElement('option');
        element.value = value;
        element.textContent = label || value;
        element.selected = selected;
        return element;
    }

    function select(values, selected, multiple) {
        var element = document.createElement('select');
        element.className = 'emby-select-withcolor emby-select';
        element.multiple = !!multiple;
        values.forEach(function (value) {
            element.appendChild(option(value, value, multiple ? selected.indexOf(value) !== -1 : value === selected));
        });
        return element;
    }

    function input(type, value, className) {
        var element = document.createElement('input');
        element.type = type;
        element.className = className || 'emby-input';
        if (type === 'number') {
            element.min = '1';
            element.step = '1';
        }

        if (type === 'checkbox') {
            element.checked = !!value;
        } else {
            element.value = value === null || value === undefined ? '' : value;
        }

        return element;
    }

    function checkbox(value) {
        return input('checkbox', value, '');
    }

    function selectedValues(element) {
        return Array.prototype.map.call(element.selectedOptions, function (selected) {
            return selected.value;
        });
    }

    function removeButton(onClick) {
        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'emby-button td-icon-button';
        button.textContent = 'X';
        button.title = 'Remove';
        button.addEventListener('click', onClick);
        return button;
    }

    function renderProfiles(page) {
        var body = qs(page, '#td-profile-rows');
        body.innerHTML = '';

        state.CapabilityProfiles.forEach(function (profile, index) {
            var row = document.createElement('tr');
            row.dataset.profileId = profile.Id;

            var name = input('text', profile.Name);
            var backend = select(backends, profile.Backend, false);
            var profileVideo = select(videoCodecs, profile.AllowedVideoCodecs, true);
            var profileAudio = select(audioCodecs, profile.AllowedAudioCodecs, true);
            var profileContainers = select(containers, profile.AllowedContainers, true);
            var device = input('text', profile.DevicePath);
            var toneMap = checkbox(profile.SupportsToneMapping);
            var subtitles = checkbox(profile.SupportsSubtitleBurnIn);

            [
                name,
                backend,
                profileVideo,
                profileAudio,
                profileContainers,
                device,
                toneMap,
                subtitles,
                removeButton(function () {
                    if (state.CapabilityProfiles.length === 1) {
                        return;
                    }

                    var replacementId = state.CapabilityProfiles[index === 0 ? 1 : 0].Id;
                    state.Presets.forEach(function (preset) {
                        if (preset.CapabilityProfileId === profile.Id) {
                            preset.CapabilityProfileId = replacementId;
                        }
                    });
                    state.CapabilityProfiles.splice(index, 1);
                    render(page);
                })
            ].forEach(function (element) {
                var cell = document.createElement('td');
                cell.appendChild(element);
                row.appendChild(cell);
            });

            name.addEventListener('input', function () {
                profile.Name = name.value;
                renderPresets(page);
            });
            backend.addEventListener('change', function () {
                profile.Backend = backend.value;
            });
            profileVideo.addEventListener('change', function () {
                profile.AllowedVideoCodecs = selectedValues(profileVideo);
            });
            profileAudio.addEventListener('change', function () {
                profile.AllowedAudioCodecs = selectedValues(profileAudio);
            });
            profileContainers.addEventListener('change', function () {
                profile.AllowedContainers = selectedValues(profileContainers);
            });
            device.addEventListener('input', function () {
                profile.DevicePath = device.value;
            });
            toneMap.addEventListener('change', function () {
                profile.SupportsToneMapping = toneMap.checked;
            });
            subtitles.addEventListener('change', function () {
                profile.SupportsSubtitleBurnIn = subtitles.checked;
            });

            body.appendChild(row);
        });
    }

    function profileOptions(selected) {
        return state.CapabilityProfiles.map(function (profile) {
            return {
                id: profile.Id,
                name: profile.Name || profile.Id,
                selected: profile.Id === selected
            };
        });
    }

    function renderPresets(page) {
        var body = qs(page, '#td-preset-rows');
        body.innerHTML = '';

        state.Presets.forEach(function (preset, index) {
            var row = document.createElement('tr');
            var name = input('text', preset.Name);
            var profile = document.createElement('select');
            profile.className = 'emby-select-withcolor emby-select';
            profileOptions(preset.CapabilityProfileId).forEach(function (entry) {
                profile.appendChild(option(entry.id, entry.name, entry.selected));
            });
            var container = select(containers, preset.Container, false);
            var video = select(videoCodecs, preset.VideoCodec, false);
            var audio = select(audioCodecs, preset.AudioCodec, false);
            var size = input('text', [preset.MaxWidth || '', preset.MaxHeight || ''].join('x'));
            var bitrate = input('text', [preset.VideoBitrateKbps || '', preset.AudioBitrateKbps || ''].join('/'));
            var enabled = checkbox(preset.Enabled);

            [
                name,
                profile,
                container,
                video,
                audio,
                size,
                bitrate,
                enabled,
                removeButton(function () {
                    if (state.Presets.length === 1) {
                        return;
                    }

                    state.Presets.splice(index, 1);
                    renderPresets(page);
                })
            ].forEach(function (element) {
                var cell = document.createElement('td');
                cell.appendChild(element);
                row.appendChild(cell);
            });

            name.addEventListener('input', function () {
                preset.Name = name.value;
            });
            profile.addEventListener('change', function () {
                preset.CapabilityProfileId = profile.value;
            });
            container.addEventListener('change', function () {
                preset.Container = container.value;
            });
            video.addEventListener('change', function () {
                preset.VideoCodec = video.value;
                preset.IsAudioOnlyPreset = video.value === 'Copy' && (preset.Container === 'Mp3' || preset.Container === 'M4a' || preset.Container === 'Ogg' || preset.Container === 'Flac');
                preset.IsVideoPreset = !preset.IsAudioOnlyPreset;
            });
            audio.addEventListener('change', function () {
                preset.AudioCodec = audio.value;
            });
            size.addEventListener('input', function () {
                var parts = size.value.split('x');
                preset.MaxWidth = optionalPositiveInt(parts[0]);
                preset.MaxHeight = optionalPositiveInt(parts[1]);
            });
            bitrate.addEventListener('input', function () {
                var parts = bitrate.value.split('/');
                preset.VideoBitrateKbps = optionalPositiveInt(parts[0]);
                preset.AudioBitrateKbps = optionalPositiveInt(parts[1]);
            });
            enabled.addEventListener('change', function () {
                preset.Enabled = enabled.checked;
            });

            body.appendChild(row);
        });
    }

    function render(page) {
        qs(page, '#td-enable-video').checked = state.EnableVideoDownloads;
        qs(page, '#td-enable-music').checked = state.EnableMusicDownloads;
        qs(page, '#td-enable-web-ui').checked = state.EnableWebUiInjection;
        qs(page, '#td-enable-advanced-ffmpeg').checked = state.EnableAdvancedUnsafeFfmpegArguments;
        qs(page, '#td-temp-directory').value = state.TempDirectory;
        qs(page, '#td-max-concurrent').value = state.MaxConcurrentJobs;
        qs(page, '#td-max-queue').value = state.MaxQueueSize;
        qs(page, '#td-retention-hours').value = state.JobRetentionHours;
        qs(page, '#td-max-temp-size').value = state.MaxTempFolderSizeMb;
        renderProfiles(page);
        renderPresets(page);
    }

    function collect(page) {
        state.EnableVideoDownloads = qs(page, '#td-enable-video').checked;
        state.EnableMusicDownloads = qs(page, '#td-enable-music').checked;
        state.EnableWebUiInjection = qs(page, '#td-enable-web-ui').checked;
        state.EnableAdvancedUnsafeFfmpegArguments = qs(page, '#td-enable-advanced-ffmpeg').checked;
        state.TempDirectory = qs(page, '#td-temp-directory').value.trim();
        state.MaxConcurrentJobs = positiveInt(qs(page, '#td-max-concurrent').value, 1);
        state.MaxQueueSize = positiveInt(qs(page, '#td-max-queue').value, 10);
        state.JobRetentionHours = positiveInt(qs(page, '#td-retention-hours').value, 24);
        state.MaxTempFolderSizeMb = positiveInt(qs(page, '#td-max-temp-size').value, 50000);

        state.CapabilityProfiles.forEach(function (profile) {
            if (!profile.AllowedVideoCodecs.length) {
                profile.AllowedVideoCodecs = ['H264'];
            }

            if (!profile.AllowedAudioCodecs.length) {
                profile.AllowedAudioCodecs = ['Aac'];
            }

            if (!profile.AllowedContainers.length) {
                profile.AllowedContainers = ['Mp4'];
            }
        });

        state.Presets = state.Presets.map(function (preset) {
            return normalizePreset(preset, state.CapabilityProfiles[0].Id);
        });
        return state;
    }

    function load(page) {
        setStatus(page, 'Loading...');
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            state = normalizeConfig(config);
            render(page);
            setStatus(page, '');
        }).catch(function () {
            state = normalizeConfig({});
            render(page);
            setStatus(page, 'Using defaults.');
        });
    }

    function bind(page) {
        qs(page, '#td-add-profile').addEventListener('click', function () {
            var profile = defaultProfile();
            profile.Id = id();
            profile.Name = 'CPU H.264';
            state.CapabilityProfiles.push(profile);
            render(page);
        });

        qs(page, '#td-add-preset').addEventListener('click', function () {
            state.Presets.push(addedDefaultPreset(state.CapabilityProfiles[0].Id));
            renderPresets(page);
        });

        qs(page, '#td-reset-defaults').addEventListener('click', function () {
            state = normalizeConfig({
                CapabilityProfiles: [defaultProfile()],
                Presets: [defaultPreset('cpu-h264-aac')]
            });
            render(page);
            setStatus(page, 'Defaults restored.');
        });

        qs(page, '#transcoded-downloads-config-form').addEventListener('submit', function (event) {
            event.preventDefault();
            setStatus(page, 'Saving...');
            ApiClient.updatePluginConfiguration(pluginId, collect(page)).then(function (result) {
                setStatus(page, 'Saved.');
                if (window.Dashboard && Dashboard.processPluginConfigurationUpdateResult) {
                    Dashboard.processPluginConfigurationUpdateResult(result);
                }
            }).catch(function () {
                setStatus(page, 'Save failed.');
            });
            return false;
        });
    }

    function initialize(page) {
        if (!page || page.id !== 'transcoded-downloads-config-page') {
            return;
        }

        if (!page.dataset.bound) {
            bind(page);
            page.dataset.bound = 'true';
        }

        load(page);
    }

    document.addEventListener('pageshow', function (event) {
        initialize(event.target);
    });

    initialize(document.getElementById('transcoded-downloads-config-page'));
})();
