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
    var popularPresetTemplates = [
        { id: '480p-h264-aac-mp4', name: '480p H.264 AAC MP4', width: 854, height: 480, videoBitrate: 2000, audioBitrate: 128, video: 'H264', audio: 'Aac', container: 'Mp4' },
        { id: '720p-h264-aac-mp4', name: '720p H.264 AAC MP4', width: 1280, height: 720, videoBitrate: 4000, audioBitrate: 160, video: 'H264', audio: 'Aac', container: 'Mp4' },
        { id: '1080p-h264-aac-mp4', name: '1080p H.264 AAC MP4', width: 1920, height: 1080, videoBitrate: 8000, audioBitrate: 192, video: 'H264', audio: 'Aac', container: 'Mp4' },
        { id: '1440p-h264-aac-mp4', name: '1440p H.264 AAC MP4', width: 2560, height: 1440, videoBitrate: 16000, audioBitrate: 192, video: 'H264', audio: 'Aac', container: 'Mp4' },
        { id: '2160p-h264-aac-mp4', name: '4K H.264 AAC MP4', width: 3840, height: 2160, videoBitrate: 35000, audioBitrate: 256, video: 'H264', audio: 'Aac', container: 'Mp4' },
        { id: '1080p-h265-aac-mp4', name: '1080p H.265 AAC MP4', width: 1920, height: 1080, videoBitrate: 5000, audioBitrate: 192, video: 'H265', audio: 'Aac', container: 'Mp4' },
        { id: '2160p-h265-aac-mp4', name: '4K H.265 AAC MP4', width: 3840, height: 2160, videoBitrate: 20000, audioBitrate: 256, video: 'H265', audio: 'Aac', container: 'Mp4' },
        { id: '1080p-av1-opus-webm', name: '1080p AV1 Opus WebM', width: 1920, height: 1080, videoBitrate: 3500, audioBitrate: 160, video: 'Av1', audio: 'Opus', container: 'Webm' }
    ];
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

    function clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
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
            Name: 'CPU common codecs',
            Backend: 'Software',
            AllowedVideoCodecs: ['H264', 'H265', 'Av1', 'Vp9'],
            AllowedAudioCodecs: ['Aac', 'Opus'],
            AllowedContainers: ['Mp4', 'Mkv', 'Webm'],
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
        return presetFromTemplate(popularPresetTemplates[2], profileId, popularPresetTemplates[2].id);
    }

    function presetFromTemplate(template, profileId, presetId) {
        return {
            Id: presetId || template.id,
            Name: template.name,
            Enabled: true,
            CapabilityProfileId: profileId,
            Container: template.container,
            VideoCodec: template.video,
            AudioCodec: template.audio,
            MaxWidth: template.width,
            MaxHeight: template.height,
            VideoBitrateKbps: template.videoBitrate,
            AudioBitrateKbps: template.audioBitrate,
            AudioChannels: 2,
            AllowStreamCopyWhenCompatible: true,
            BurnSubtitles: false,
            ToneMapHdrToSdr: true,
            PreserveOriginalAudioIfCompatible: false,
            IsVideoPreset: true,
            IsAudioOnlyPreset: false
        };
    }

    function defaultPopularPresets(profileId) {
        return popularPresetTemplates.map(function (template) {
            return presetFromTemplate(template, profileId, template.id);
        });
    }

    function addedDefaultPreset(profileId) {
        var preset = defaultPreset(profileId);
        preset.Id = id();
        return preset;
    }

    function simpleH264Preset(profileId, resolution, bitrateMbps) {
        var parts = resolution.split('x');
        var width = positiveInt(parts[0], 1920);
        var height = positiveInt(parts[1], 1080);
        var bitrate = clamp(positiveInt(bitrateMbps, 8), 2, 80);
        return {
            Id: id(),
            Name: height + 'p H.264 AAC MP4 ' + bitrate + ' Mbps',
            Enabled: true,
            CapabilityProfileId: profileId,
            Container: 'Mp4',
            VideoCodec: 'H264',
            AudioCodec: 'Aac',
            MaxWidth: width,
            MaxHeight: height,
            VideoBitrateKbps: bitrate * 1000,
            AudioBitrateKbps: height >= 2160 ? 256 : height >= 1080 ? 192 : 160,
            AudioChannels: 2,
            AllowStreamCopyWhenCompatible: true,
            BurnSubtitles: false,
            ToneMapHdrToSdr: true,
            PreserveOriginalAudioIfCompatible: false,
            IsVideoPreset: true,
            IsAudioOnlyPreset: false
        };
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
            presets = defaultPopularPresets(profiles[0].Id);
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

    function field(label, element, wide) {
        var container = document.createElement('div');
        var labelElement = document.createElement('label');

        container.className = 'inputContainer' + (wide ? ' td-field-wide' : '');
        labelElement.className = 'inputLabel inputLabelUnfocused';
        labelElement.textContent = label;
        container.appendChild(labelElement);
        container.appendChild(element);
        return container;
    }

    function check(label, element) {
        var container = document.createElement('label');
        var text = document.createElement('span');

        container.className = 'td-check';
        text.textContent = label;
        container.appendChild(element);
        container.appendChild(text);
        return container;
    }

    function detailsRow(title, subtitle, chipValues, remove) {
        var details = document.createElement('details');
        var summary = document.createElement('summary');
        var main = document.createElement('span');
        var titleElement = document.createElement('span');
        var subtitleElement = document.createElement('span');
        var actions = document.createElement('span');
        var body = document.createElement('div');

        details.className = 'td-details';
        summary.className = 'td-details-summary';
        main.className = 'td-row-main';
        titleElement.className = 'td-row-title';
        subtitleElement.className = 'td-row-subtitle';
        actions.className = 'td-row-actions';
        body.className = 'td-details-body';

        titleElement.textContent = title;
        subtitleElement.textContent = subtitle;
        main.appendChild(titleElement);
        main.appendChild(subtitleElement);

        summary.appendChild(main);
        summary.appendChild(chips(chipValues));
        actions.appendChild(remove);
        summary.appendChild(actions);
        details.appendChild(summary);
        details.appendChild(body);

        return {
            details: details,
            body: body
        };
    }

    function chips(values) {
        var row = document.createElement('div');
        row.className = 'td-chip-row';
        ensureArray(values).forEach(function (value) {
            var chip = document.createElement('span');
            chip.className = 'td-chip';
            chip.textContent = value;
            row.appendChild(chip);
        });
        return row;
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
        button.title = 'Remove';
        button.innerHTML = '<span class="material-icons" aria-hidden="true">delete</span>';
        button.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            onClick();
        });
        return button;
    }

    function profileName(profileId) {
        var profile = state.CapabilityProfiles.filter(function (candidate) {
            return candidate.Id === profileId;
        })[0];

        return profile ? profile.Name || profile.Id : profileId;
    }

    function presetResolution(preset) {
        if (preset.IsAudioOnlyPreset) {
            return 'Audio';
        }

        if (preset.MaxWidth && preset.MaxHeight) {
            return preset.MaxWidth + 'x' + preset.MaxHeight;
        }

        return 'Original';
    }

    function presetBitrate(preset) {
        var parts = [];
        if (preset.VideoBitrateKbps && !preset.IsAudioOnlyPreset) {
            parts.push(preset.VideoBitrateKbps + ' video');
        }

        if (preset.AudioBitrateKbps) {
            parts.push(preset.AudioBitrateKbps + ' audio');
        }

        return parts.length ? parts.join(' / ') : 'Auto';
    }

    function syncGeneral(page) {
        if (!state) {
            return;
        }

        state.EnableVideoDownloads = qs(page, '#td-enable-video').checked;
        state.EnableMusicDownloads = qs(page, '#td-enable-music').checked;
        state.EnableWebUiInjection = qs(page, '#td-enable-web-ui').checked;
        state.EnableAdvancedUnsafeFfmpegArguments = qs(page, '#td-enable-advanced-ffmpeg').checked;
        state.TempDirectory = qs(page, '#td-temp-directory').value.trim();
        state.MaxConcurrentJobs = positiveInt(qs(page, '#td-max-concurrent').value, 1);
        state.MaxQueueSize = positiveInt(qs(page, '#td-max-queue').value, 10);
        state.JobRetentionHours = positiveInt(qs(page, '#td-retention-hours').value, 24);
        state.MaxTempFolderSizeMb = positiveInt(qs(page, '#td-max-temp-size').value, 50000);
    }

    function updateSummary(page) {
        if (!state) {
            return;
        }

        qs(page, '#td-summary-presets').textContent = state.Presets.filter(function (preset) {
            return preset.Enabled !== false;
        }).length + '/' + state.Presets.length;
        qs(page, '#td-summary-profiles').textContent = state.CapabilityProfiles.length;
        qs(page, '#td-summary-concurrency').textContent = state.MaxConcurrentJobs;
        qs(page, '#td-summary-retention').textContent = state.JobRetentionHours + 'h';
    }

    function renderProfiles(page) {
        var body = qs(page, '#td-profile-rows');
        body.innerHTML = '';

        if (!state.CapabilityProfiles.length) {
            var empty = document.createElement('div');
            empty.className = 'td-empty';
            empty.textContent = 'No capability profiles configured.';
            body.appendChild(empty);
            return;
        }

        state.CapabilityProfiles.forEach(function (profile, index) {
            var grid = document.createElement('div');
            var checks = document.createElement('div');

            var name = input('text', profile.Name);
            var backend = select(backends, profile.Backend, false);
            var profileVideo = select(videoCodecs, profile.AllowedVideoCodecs, true);
            var profileAudio = select(audioCodecs, profile.AllowedAudioCodecs, true);
            var profileContainers = select(containers, profile.AllowedContainers, true);
            var device = input('text', profile.DevicePath);
            var notes = input('text', profile.Notes);
            var hardwareDecode = checkbox(profile.SupportsHardwareDecode);
            var hardwareEncode = checkbox(profile.SupportsHardwareEncode);
            var toneMap = checkbox(profile.SupportsToneMapping);
            var subtitles = checkbox(profile.SupportsSubtitleBurnIn);
            var twoPass = checkbox(profile.SupportsTwoPass);
            var remove = removeButton(function () {
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
            });
            var row = detailsRow(
                profile.Name || 'Capability profile',
                profile.Backend + ' backend',
                [
                    profile.Backend,
                    profile.AllowedVideoCodecs.length + ' video',
                    profile.AllowedAudioCodecs.length + ' audio',
                    profile.AllowedContainers.length + ' containers'
                ],
                remove);

            grid.className = 'td-field-grid';
            checks.className = 'td-inline-checks';

            grid.appendChild(field('Name', name, false));
            grid.appendChild(field('Backend', backend, false));
            grid.appendChild(field('Video codecs', profileVideo, false));
            grid.appendChild(field('Audio codecs', profileAudio, false));
            grid.appendChild(field('Containers', profileContainers, false));
            grid.appendChild(field('Device path', device, false));
            grid.appendChild(field('Notes', notes, true));

            checks.appendChild(check('Hardware decode', hardwareDecode));
            checks.appendChild(check('Hardware encode', hardwareEncode));
            checks.appendChild(check('Tone mapping', toneMap));
            checks.appendChild(check('Subtitle burn-in', subtitles));
            checks.appendChild(check('Two pass', twoPass));

            row.body.appendChild(grid);
            row.body.appendChild(checks);

            name.addEventListener('input', function () {
                profile.Name = name.value;
                renderPresets(page);
                updateSummary(page);
            });
            backend.addEventListener('change', function () {
                profile.Backend = backend.value;
                renderProfiles(page);
            });
            profileVideo.addEventListener('change', function () {
                profile.AllowedVideoCodecs = selectedValues(profileVideo);
                renderProfiles(page);
            });
            profileAudio.addEventListener('change', function () {
                profile.AllowedAudioCodecs = selectedValues(profileAudio);
                renderProfiles(page);
            });
            profileContainers.addEventListener('change', function () {
                profile.AllowedContainers = selectedValues(profileContainers);
                renderProfiles(page);
            });
            device.addEventListener('input', function () {
                profile.DevicePath = device.value;
            });
            notes.addEventListener('input', function () {
                profile.Notes = notes.value;
            });
            hardwareDecode.addEventListener('change', function () {
                profile.SupportsHardwareDecode = hardwareDecode.checked;
            });
            hardwareEncode.addEventListener('change', function () {
                profile.SupportsHardwareEncode = hardwareEncode.checked;
            });
            toneMap.addEventListener('change', function () {
                profile.SupportsToneMapping = toneMap.checked;
            });
            subtitles.addEventListener('change', function () {
                profile.SupportsSubtitleBurnIn = subtitles.checked;
            });
            twoPass.addEventListener('change', function () {
                profile.SupportsTwoPass = twoPass.checked;
            });

            body.appendChild(row.details);
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

        if (!state.Presets.length) {
            var empty = document.createElement('div');
            empty.className = 'td-empty';
            empty.textContent = 'No presets configured.';
            body.appendChild(empty);
            return;
        }

        state.Presets.forEach(function (preset, index) {
            var grid = document.createElement('div');
            var checks = document.createElement('div');
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
            var audioChannels = input('number', preset.AudioChannels);
            var enabled = checkbox(preset.Enabled);
            var streamCopy = checkbox(preset.AllowStreamCopyWhenCompatible);
            var burnSubtitles = checkbox(preset.BurnSubtitles);
            var toneMap = checkbox(preset.ToneMapHdrToSdr);
            var preserveAudio = checkbox(preset.PreserveOriginalAudioIfCompatible);
            var audioOnly = checkbox(preset.IsAudioOnlyPreset);
            var remove = removeButton(function () {
                if (state.Presets.length === 1) {
                    return;
                }

                state.Presets.splice(index, 1);
                renderPresets(page);
                updateSummary(page);
            });
            var row = detailsRow(
                preset.Name || 'Transcode preset',
                profileName(preset.CapabilityProfileId) + ' · ' + preset.Container + ' · ' + presetResolution(preset) + ' · ' + presetBitrate(preset),
                [
                    preset.Enabled ? 'Enabled' : 'Disabled',
                    preset.Container,
                    preset.IsAudioOnlyPreset ? 'Audio' : preset.VideoCodec,
                    preset.IsAudioOnlyPreset ? preset.AudioCodec : presetResolution(preset)
                ],
                remove);

            grid.className = 'td-field-grid';
            checks.className = 'td-inline-checks';

            grid.appendChild(field('Name', name, true));
            grid.appendChild(field('Profile', profile, false));
            grid.appendChild(field('Container', container, false));
            grid.appendChild(field('Video codec', video, false));
            grid.appendChild(field('Audio codec', audio, false));
            grid.appendChild(field('Size, WxH', size, false));
            grid.appendChild(field('Bitrate, video/audio', bitrate, false));
            grid.appendChild(field('Audio channels', audioChannels, false));

            checks.appendChild(check('Enabled', enabled));
            checks.appendChild(check('Audio only', audioOnly));
            checks.appendChild(check('Stream copy when compatible', streamCopy));
            checks.appendChild(check('Burn subtitles', burnSubtitles));
            checks.appendChild(check('Tone map HDR to SDR', toneMap));
            checks.appendChild(check('Preserve original audio', preserveAudio));

            row.body.appendChild(grid);
            row.body.appendChild(checks);

            name.addEventListener('input', function () {
                preset.Name = name.value;
            });
            profile.addEventListener('change', function () {
                preset.CapabilityProfileId = profile.value;
                renderPresets(page);
            });
            container.addEventListener('change', function () {
                preset.Container = container.value;
                preset.IsAudioOnlyPreset = video.value === 'Copy' && (preset.Container === 'Mp3' || preset.Container === 'M4a' || preset.Container === 'Ogg' || preset.Container === 'Flac');
                preset.IsVideoPreset = !preset.IsAudioOnlyPreset;
                renderPresets(page);
            });
            video.addEventListener('change', function () {
                preset.VideoCodec = video.value;
                preset.IsAudioOnlyPreset = video.value === 'Copy' && (preset.Container === 'Mp3' || preset.Container === 'M4a' || preset.Container === 'Ogg' || preset.Container === 'Flac');
                preset.IsVideoPreset = !preset.IsAudioOnlyPreset;
                renderPresets(page);
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
            audioChannels.addEventListener('input', function () {
                preset.AudioChannels = optionalPositiveInt(audioChannels.value);
            });
            enabled.addEventListener('change', function () {
                preset.Enabled = enabled.checked;
                renderPresets(page);
                updateSummary(page);
            });
            audioOnly.addEventListener('change', function () {
                preset.IsAudioOnlyPreset = audioOnly.checked;
                preset.IsVideoPreset = !audioOnly.checked;
                if (audioOnly.checked) {
                    preset.VideoCodec = 'Copy';
                    preset.MaxWidth = null;
                    preset.MaxHeight = null;
                    preset.VideoBitrateKbps = null;
                    preset.BurnSubtitles = false;
                    preset.ToneMapHdrToSdr = false;
                }

                renderPresets(page);
            });
            streamCopy.addEventListener('change', function () {
                preset.AllowStreamCopyWhenCompatible = streamCopy.checked;
            });
            burnSubtitles.addEventListener('change', function () {
                preset.BurnSubtitles = burnSubtitles.checked;
            });
            toneMap.addEventListener('change', function () {
                preset.ToneMapHdrToSdr = toneMap.checked;
            });
            preserveAudio.addEventListener('change', function () {
                preset.PreserveOriginalAudioIfCompatible = preserveAudio.checked;
            });

            body.appendChild(row.details);
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
        qs(page, '#td-simple-bitrate-number').value = qs(page, '#td-simple-bitrate').value;
        renderProfiles(page);
        renderPresets(page);
        updateSummary(page);
    }

    function collect(page) {
        syncGeneral(page);

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
        [
            '#td-enable-video',
            '#td-enable-music',
            '#td-enable-web-ui',
            '#td-enable-advanced-ffmpeg',
            '#td-temp-directory',
            '#td-max-concurrent',
            '#td-max-queue',
            '#td-retention-hours',
            '#td-max-temp-size'
        ].forEach(function (selector) {
            qs(page, selector).addEventListener('input', function () {
                syncGeneral(page);
                updateSummary(page);
            });
            qs(page, selector).addEventListener('change', function () {
                syncGeneral(page);
                updateSummary(page);
            });
        });

        qs(page, '#td-simple-bitrate').addEventListener('input', function () {
            qs(page, '#td-simple-bitrate-number').value = qs(page, '#td-simple-bitrate').value;
        });

        qs(page, '#td-simple-bitrate-number').addEventListener('input', function () {
            var value = clamp(positiveInt(qs(page, '#td-simple-bitrate-number').value, 8), 2, 80);
            qs(page, '#td-simple-bitrate-number').value = value;
            qs(page, '#td-simple-bitrate').value = value;
        });

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
            updateSummary(page);
        });

        qs(page, '#td-add-simple-h264').addEventListener('click', function () {
            state.Presets.push(simpleH264Preset(
                state.CapabilityProfiles[0].Id,
                qs(page, '#td-simple-resolution').value,
                qs(page, '#td-simple-bitrate-number').value));
            renderPresets(page);
            updateSummary(page);
            setStatus(page, 'H.264 preset added. Save to apply.');
        });

        qs(page, '#td-add-popular-presets').addEventListener('click', function () {
            var existing = state.Presets.reduce(function (seen, preset) {
                seen[preset.Id] = true;
                return seen;
            }, {});
            var added = 0;
            defaultPopularPresets(state.CapabilityProfiles[0].Id).forEach(function (preset) {
                if (!existing[preset.Id]) {
                    state.Presets.push(preset);
                    added += 1;
                }
            });
            renderPresets(page);
            updateSummary(page);
            setStatus(page, added ? 'Popular presets added. Save to apply.' : 'Popular presets already exist.');
        });

        qs(page, '#td-reset-defaults').addEventListener('click', function () {
            state = normalizeConfig({
                CapabilityProfiles: [defaultProfile()],
                Presets: defaultPopularPresets('cpu-h264-aac')
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
