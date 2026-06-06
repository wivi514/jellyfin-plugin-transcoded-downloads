(function () {
    'use strict';

    if (window.TranscodedDownloadsInjected) {
        return;
    }

    window.TranscodedDownloadsInjected = true;

    var plugin = {
        buttonClass: 'transcoded-downloads-action',
        dialogId: 'transcoded-downloads-dialog',
        pollIntervalMs: 2000,
        maxPollAttempts: 900
    };

    var jobStatus = {
        queued: 0,
        running: 1,
        completed: 2,
        failed: 3,
        cancelled: 4,
        expired: 5
    };

    function apiUrl(path) {
        if (window.ApiClient && typeof window.ApiClient.getUrl === 'function') {
            return window.ApiClient.getUrl(path.replace(/^\//, ''));
        }

        return path;
    }

    function request(path, options) {
        var url = apiUrl(path);
        var requestOptions = options || {};

        if (window.ApiClient && typeof window.ApiClient.ajax === 'function') {
            return window.ApiClient.ajax({
                type: requestOptions.method || 'GET',
                url: url,
                data: requestOptions.body,
                contentType: 'application/json'
            });
        }

        requestOptions.headers = requestOptions.headers || {};
        requestOptions.headers['Content-Type'] = 'application/json';

        if (window.ApiClient && window.ApiClient.accessToken) {
            requestOptions.headers['X-Emby-Token'] = window.ApiClient.accessToken();
        }

        return fetch(url, requestOptions).then(function (response) {
            if (!response.ok) {
                return response.text().then(function (message) {
                    throw new Error(message || response.statusText);
                });
            }

            if (response.status === 204) {
                return null;
            }

            return response.json();
        });
    }

    function getItemId() {
        var query = new URLSearchParams(window.location.search);
        var id = query.get('id') || query.get('itemId') || query.get('itemid');

        if (!id && window.location.hash.indexOf('?') !== -1) {
            query = new URLSearchParams(window.location.hash.substring(window.location.hash.indexOf('?') + 1));
            id = query.get('id') || query.get('itemId') || query.get('itemid');
        }

        return id;
    }

    function findButtonContainer() {
        var selectors = [
            '.itemDetailPage .mainDetailButtons',
            '.itemDetailPage .detailButtonContainer',
            '.itemDetailPage .detailButtons',
            '.detailPagePrimaryContainer .mainDetailButtons',
            '.detailPagePrimaryContainer .detailButtonContainer',
            '.detailPageContent .detailButtonContainer'
        ];

        for (var i = 0; i < selectors.length; i++) {
            var element = document.querySelector(selectors[i]);
            if (element) {
                return element;
            }
        }

        return null;
    }

    function ensureStyles() {
        if (document.getElementById('transcoded-downloads-style')) {
            return;
        }

        var style = document.createElement('style');
        style.id = 'transcoded-downloads-style';
        style.textContent = [
            '.transcoded-downloads-modal-backdrop{position:fixed;inset:0;z-index:999999;background:rgba(0,0,0,.58);display:flex;align-items:center;justify-content:center;padding:24px;}',
            '.transcoded-downloads-modal{width:min(520px,100%);max-height:min(720px,90vh);overflow:auto;background:#202020;color:#fff;border-radius:8px;box-shadow:0 18px 60px rgba(0,0,0,.45);}',
            '.transcoded-downloads-modal-header{display:flex;align-items:center;justify-content:space-between;gap:16px;padding:20px 22px 10px;}',
            '.transcoded-downloads-title{font-size:1.25rem;font-weight:600;margin:0;}',
            '.transcoded-downloads-close{appearance:none;border:0;background:transparent;color:inherit;font-size:28px;line-height:1;cursor:pointer;}',
            '.transcoded-downloads-body{padding:12px 22px 22px;}',
            '.transcoded-downloads-select{width:100%;box-sizing:border-box;margin:8px 0 16px;padding:10px;border-radius:4px;border:1px solid rgba(255,255,255,.28);background:#111;color:#fff;}',
            '.transcoded-downloads-existing{margin:0 0 18px;padding:12px;border:1px solid rgba(255,255,255,.14);border-radius:6px;background:rgba(255,255,255,.05);}',
            '.transcoded-downloads-existing-title{font-size:.95rem;font-weight:600;margin:0 0 8px;}',
            '.transcoded-downloads-job-row{display:grid;grid-template-columns:1fr auto;gap:8px;align-items:center;margin-top:8px;}',
            '.transcoded-downloads-job-name{font-size:.9rem;color:rgba(255,255,255,.88);}',
            '.transcoded-downloads-job-meta{font-size:.8rem;color:rgba(255,255,255,.62);margin-top:2px;}',
            '.transcoded-downloads-status{min-height:1.4em;margin:10px 0 0;color:rgba(255,255,255,.78);}',
            '.transcoded-downloads-actions{display:flex;gap:12px;justify-content:flex-end;margin-top:18px;flex-wrap:wrap;}',
            '.transcoded-downloads-primary,.transcoded-downloads-secondary{border:0;border-radius:4px;padding:10px 16px;cursor:pointer;}',
            '.transcoded-downloads-primary{background:#00a4dc;color:#fff;}',
            '.transcoded-downloads-secondary{background:rgba(255,255,255,.16);color:#fff;}',
            '.transcoded-downloads-primary:disabled{opacity:.58;cursor:default;}'
        ].join('');

        document.head.appendChild(style);
    }

    function showToast(message) {
        if (window.Dashboard && typeof window.Dashboard.alert === 'function') {
            window.Dashboard.alert(message);
            return;
        }

        window.alert(message);
    }

    function createButton() {
        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'detailButton emby-button ' + plugin.buttonClass;
        button.title = 'Transcoded Download';
        button.setAttribute('is', 'emby-button');
        button.innerHTML = '<span class="material-icons detailButton-icon">download</span><div class="detailButton-text">Transcoded</div>';
        button.addEventListener('click', openDialog);
        return button;
    }

    function injectButton() {
        if (!getItemId() || document.querySelector('.' + plugin.buttonClass)) {
            return;
        }

        var container = findButtonContainer();
        if (!container) {
            return;
        }

        container.appendChild(createButton());
    }

    function removeDialog() {
        var existing = document.getElementById(plugin.dialogId);
        if (existing) {
            existing.parentNode.removeChild(existing);
        }
    }

    function setStatus(dialog, message) {
        var status = dialog.querySelector('.transcoded-downloads-status');
        if (status) {
            status.textContent = message || '';
        }
    }

    function describePreset(preset) {
        var parts = [];
        var name = getField(preset, 'name', 'Name');
        var maxWidth = getField(preset, 'maxWidth', 'MaxWidth');
        var maxHeight = getField(preset, 'maxHeight', 'MaxHeight');
        var videoBitrate = getField(preset, 'videoBitrateKbps', 'VideoBitrateKbps');
        var audioBitrate = getField(preset, 'audioBitrateKbps', 'AudioBitrateKbps');

        if (maxWidth && maxHeight) {
            parts.push(maxWidth + 'x' + maxHeight);
        }

        if (videoBitrate) {
            parts.push(videoBitrate + ' kbps video');
        }

        if (audioBitrate) {
            parts.push(audioBitrate + ' kbps audio');
        }

        return parts.length ? name + ' (' + parts.join(', ') + ')' : name;
    }

    function getField(value, camelName, pascalName) {
        if (!value) {
            return undefined;
        }

        return value[camelName] === undefined ? value[pascalName] : value[camelName];
    }

    function isSameGuid(left, right) {
        return String(left || '').toLowerCase() === String(right || '').toLowerCase();
    }

    function normalizeStatus(value) {
        if (typeof value === 'string') {
            return jobStatus[value.charAt(0).toLowerCase() + value.slice(1)];
        }

        return value;
    }

    function statusLabel(value) {
        switch (normalizeStatus(value)) {
            case jobStatus.queued:
                return 'Queued';
            case jobStatus.running:
                return 'Running';
            case jobStatus.completed:
                return 'Ready';
            case jobStatus.failed:
                return 'Failed';
            case jobStatus.cancelled:
                return 'Cancelled';
            case jobStatus.expired:
                return 'Expired';
            default:
                return 'Unknown';
        }
    }

    function formatBytes(bytes) {
        var value = Number(bytes);
        var units = ['B', 'KB', 'MB', 'GB'];
        var index = 0;

        while (value >= 1024 && index < units.length - 1) {
            value = value / 1024;
            index++;
        }

        return (index === 0 ? value : value.toFixed(1)) + ' ' + units[index];
    }

    function formatJobMeta(job) {
        var parts = [statusLabel(getField(job, 'status', 'Status'))];
        var progress = getField(job, 'progressPercent', 'ProgressPercent');
        var size = getField(job, 'outputSizeBytes', 'OutputSizeBytes');

        if (normalizeStatus(getField(job, 'status', 'Status')) !== jobStatus.completed && progress) {
            parts.push(Math.round(progress) + '%');
        }

        if (size) {
            parts.push(formatBytes(size));
        }

        return parts.join(' - ');
    }

    function getPresetName(presets, presetId) {
        for (var i = 0; i < presets.length; i++) {
            if (getField(presets[i], 'id', 'Id') === presetId) {
                return getField(presets[i], 'name', 'Name');
            }
        }

        return presetId;
    }

    function getItemJobs(jobs) {
        var itemId = getItemId();

        return (jobs || []).filter(function (job) {
            return isSameGuid(getField(job, 'itemId', 'ItemId'), itemId);
        }).sort(function (left, right) {
            return Date.parse(getField(right, 'createdAt', 'CreatedAt') || 0) - Date.parse(getField(left, 'createdAt', 'CreatedAt') || 0);
        });
    }

    function renderExistingJobs(dialog, jobs, presets) {
        var container = dialog.querySelector('.transcoded-downloads-existing');
        if (!container) {
            return;
        }

        if (!jobs.length) {
            container.hidden = true;
            container.innerHTML = '';
            return;
        }

        container.hidden = false;
        container.innerHTML = '<p class="transcoded-downloads-existing-title">Existing jobs</p>';

        jobs.slice(0, 3).forEach(function (job) {
            var row = document.createElement('div');
            var action = document.createElement('button');
            var status = normalizeStatus(getField(job, 'status', 'Status'));
            var jobId = getField(job, 'id', 'Id');

            row.className = 'transcoded-downloads-job-row';
            row.innerHTML = [
                '<div>',
                '<div class="transcoded-downloads-job-name">' + escapeHtml(getPresetName(presets, getField(job, 'presetId', 'PresetId'))) + '</div>',
                '<div class="transcoded-downloads-job-meta">' + escapeHtml(formatJobMeta(job)) + '</div>',
                '</div>'
            ].join('');

            action.type = 'button';
            action.className = status === jobStatus.completed ? 'transcoded-downloads-primary' : 'transcoded-downloads-secondary';

            if (status === jobStatus.completed) {
                action.textContent = 'Download';
                action.addEventListener('click', function () {
                    downloadJob(jobId);
                    removeDialog();
                });
            } else if (status === jobStatus.queued || status === jobStatus.running) {
                action.textContent = 'Resume';
                action.addEventListener('click', function () {
                    pollJob(dialog, jobId, 0);
                });
            } else {
                action.textContent = 'Remove';
                action.addEventListener('click', function () {
                    deleteJob(dialog, jobId, presets);
                });
            }

            row.appendChild(action);
            container.appendChild(row);
        });
    }

    function renderDialog(presets, jobs) {
        removeDialog();
        ensureStyles();

        var backdrop = document.createElement('div');
        backdrop.id = plugin.dialogId;
        backdrop.className = 'transcoded-downloads-modal-backdrop';
        backdrop.transcodedDownloadsPresets = presets;

        var options = presets.map(function (preset) {
            return '<option value="' + encodeURIComponent(getField(preset, 'id', 'Id')) + '">' + escapeHtml(describePreset(preset)) + '</option>';
        }).join('');

        backdrop.innerHTML = [
            '<div class="transcoded-downloads-modal" role="dialog" aria-modal="true" aria-labelledby="transcoded-downloads-title">',
            '<div class="transcoded-downloads-modal-header">',
            '<h2 id="transcoded-downloads-title" class="transcoded-downloads-title">Transcoded Download</h2>',
            '<button type="button" class="transcoded-downloads-close" aria-label="Close">&times;</button>',
            '</div>',
            '<div class="transcoded-downloads-body">',
            '<div class="transcoded-downloads-existing" hidden></div>',
            '<select class="transcoded-downloads-select" aria-label="Preset">' + options + '</select>',
            '<div class="transcoded-downloads-status"></div>',
            '<div class="transcoded-downloads-actions">',
            '<button type="button" class="transcoded-downloads-secondary">Cancel</button>',
            '<button type="button" class="transcoded-downloads-primary">Start</button>',
            '</div>',
            '</div>',
            '</div>'
        ].join('');

        backdrop.querySelector('.transcoded-downloads-close').addEventListener('click', removeDialog);
        backdrop.querySelector('.transcoded-downloads-secondary').addEventListener('click', removeDialog);
        backdrop.querySelector('.transcoded-downloads-primary').addEventListener('click', function () {
            startDownload(backdrop);
        });

        document.body.appendChild(backdrop);
        renderExistingJobs(backdrop, getItemJobs(jobs), presets);
    }

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function openDialog() {
        Promise.all([
            request('/TranscodedDownloads/Presets'),
            request('/TranscodedDownloads/Jobs')
        ])
            .then(function (results) {
                var presets = results[0];
                var jobs = results[1];

                if (!presets || !presets.length) {
                    showToast('No transcoded download presets are available.');
                    return;
                }

                renderDialog(presets, jobs || []);
            })
            .catch(function (error) {
                showToast('Unable to load transcoded download presets. ' + (error.message || error));
            });
    }

    function startDownload(dialog) {
        var itemId = getItemId();
        var select = dialog.querySelector('.transcoded-downloads-select');
        var startButton = dialog.querySelector('.transcoded-downloads-primary');
        var presetId = decodeURIComponent(select.value);

        startButton.disabled = true;
        setStatus(dialog, 'Starting transcode...');

        request('/TranscodedDownloads/Jobs', {
            method: 'POST',
            body: JSON.stringify({
                itemId: itemId,
                presetId: presetId,
                startImmediately: true
            })
        })
            .then(function (job) {
                pollJob(dialog, getField(job, 'id', 'Id'), 0);
            })
            .catch(function (error) {
                startButton.disabled = false;
                setStatus(dialog, error.message || 'Unable to start transcode.');
            });
    }

    function downloadJob(jobId) {
        window.location.href = apiUrl('/TranscodedDownloads/Jobs/' + encodeURIComponent(jobId) + '/File');
    }

    function deleteJob(dialog, jobId, presets) {
        setStatus(dialog, 'Removing job...');

        request('/TranscodedDownloads/Jobs/' + encodeURIComponent(jobId), {
            method: 'DELETE'
        })
            .then(function () {
                return request('/TranscodedDownloads/Jobs');
            })
            .then(function (jobs) {
                setStatus(dialog, '');
                renderExistingJobs(dialog, getItemJobs(jobs), presets);
            })
            .catch(function (error) {
                setStatus(dialog, error.message || 'Unable to remove job.');
            });
    }

    function pollJob(dialog, jobId, attempt) {
        if (attempt >= plugin.maxPollAttempts) {
            setStatus(dialog, 'The transcode is still running. Check the job list later.');
            return;
        }

        request('/TranscodedDownloads/Jobs/' + encodeURIComponent(jobId))
            .then(function (job) {
                var status = normalizeStatus(getField(job, 'status', 'Status'));
                if (status === jobStatus.completed) {
                    setStatus(dialog, 'Transcode complete. Opening download...');
                    downloadJob(jobId);
                    removeDialog();
                    return;
                }

                if (status === jobStatus.failed) {
                    setStatus(dialog, getField(job, 'errorMessage', 'ErrorMessage') || 'The transcode failed.');
                    return;
                }

                if (status === jobStatus.cancelled || status === jobStatus.expired) {
                    setStatus(dialog, 'The transcode is no longer available.');
                    return;
                }

                setStatus(dialog, 'Transcoding... ' + Math.round(getField(job, 'progressPercent', 'ProgressPercent') || 0) + '%');
                renderExistingJobs(dialog, getItemJobs([job]), dialog.transcodedDownloadsPresets || []);
                window.setTimeout(function () {
                    pollJob(dialog, jobId, attempt + 1);
                }, plugin.pollIntervalMs);
            })
            .catch(function (error) {
                setStatus(dialog, error.message || 'Unable to read job status.');
            });
    }

    injectButton();

    var observer = new MutationObserver(injectButton);
    observer.observe(document.body, { childList: true, subtree: true });

    window.addEventListener('hashchange', function () {
        window.setTimeout(injectButton, 250);
    });
})();
