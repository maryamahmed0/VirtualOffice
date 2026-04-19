mergeInto(LibraryManager.library, {

    InitWebMicInterceptor: function() {
        if (window.micInterceptorInitialized) return;
        window.micInterceptorInitialized = true;

        window.activeAudioStreams = [];
        window.vivoxAudioElements = [];

        // 🎤 Intercept mic (outgoing)
        var origGetUserMedia = navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices);
        navigator.mediaDevices.getUserMedia = function(constraints) {
            return origGetUserMedia(constraints).then(function(stream) {
                if (constraints && constraints.audio) {
                    window.activeAudioStreams.push(stream);
                    console.log("[WebAudio] 🎤 Mic intercepted! tracks=" + stream.getAudioTracks().length);
                }
                return stream;
            });
        };

        // 🎧 Intercept audio elements (incoming)
        var origCreateElement = document.createElement.bind(document);
        document.createElement = function(tagName) {
            var el = origCreateElement(tagName);

            if (typeof tagName === 'string' && tagName.toLowerCase() === 'audio') {
                console.log("[WebAudio] 🎧 Audio element created!");

                el.autoplay = true;
                el.setAttribute('playsinline', '');
                el.muted = false;
                el.volume = 1.0;

                window.vivoxAudioElements.push(el);

                // ✅ حط العنصر في الـ DOM عشان المتصفح يشتغل معاه
                el.style.position = 'fixed';
                el.style.left = '-9999px';
                el.style.top = '-9999px';
                el.style.width = '1px';
                el.style.height = '1px';
                document.body.appendChild(el);

                // ✅ لما الـ track يبقى unmuted، شغله
                el.addEventListener('loadedmetadata', function() {
                    console.log("[WebAudio] metadata loaded! readyState=" + el.readyState);
                    el.muted = false;
                    el.volume = 1.0;
                    el.play().catch(function(e) {
                        console.warn("[WebAudio] play on metadata failed:", e.message);
                    });
                });

                el.addEventListener('canplay', function() {
                    el.muted = false;
                    el.volume = 1.0;
                    el.play().catch(function(e) {});
                });

                el.addEventListener('playing', function() {
                    console.log("[WebAudio] 🔊 PLAYING! readyState=" + el.readyState + " ← AUDIO FLOWING ✅");
                });

                el.addEventListener('waiting', function() {
                    console.log("[WebAudio] ⏳ WAITING readyState=" + el.readyState);
                });

                el.addEventListener('stalled', function() {
                    console.warn("[WebAudio] ⚠️ STALLED");
                });
            }

            return el;
        };

        console.log("[WebAudio] ✅ Interceptor Installed");
    },

    CleanupAudioElements: function() {
        if (!window.vivoxAudioElements) return;
        var before = window.vivoxAudioElements.length;
        window.vivoxAudioElements = window.vivoxAudioElements.filter(function(el) {
            if (!el || !el.srcObject) {
                if (el && el.parentNode) el.parentNode.removeChild(el);
                return false;
            }
            var tracks = el.srcObject.getAudioTracks();
            if (tracks.length === 0 || tracks.every(function(t) { return t.readyState === 'ended'; })) {
                if (el.parentNode) el.parentNode.removeChild(el);
                return false;
            }
            return true;
        });
        console.log("[WebAudio] 🧹 Cleanup: " + before + " → " + window.vivoxAudioElements.length);
    },

    ForceUnlockWebAudio: function() {
        document.querySelectorAll('audio').forEach(function(audioObj) {
            if (!audioObj) return;
            audioObj.muted = false;
            audioObj.volume = 1.0;
            if (audioObj.srcObject) {
                audioObj.srcObject.getAudioTracks().forEach(function(t) {
                    t.enabled = true;
                });
            }
            var p = audioObj.play();
            if (p !== undefined) p.catch(function(e) {});
        });

        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
            if (WEBAudio.audioContext.state === 'suspended') {
                WEBAudio.audioContext.resume();
                console.log("[WebAudio] 🔊 AudioContext resumed");
            }
        }
    },

    ResumeWebAudioContext: function() {
        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
            if (WEBAudio.audioContext.state === 'suspended') {
                WEBAudio.audioContext.resume();
            }
        }
    },

    HardMuteWebMic: function(mute) {
        if (window.activeAudioStreams && window.activeAudioStreams.length > 0) {
            var count = 0;
            window.activeAudioStreams.forEach(function(stream) {
                stream.getAudioTracks().forEach(function(t) {
                    t.enabled = !mute;
                    count++;
                });
            });
            console.log("[WebAudio] 🔇 HardMute=" + mute + " on " + count + " mic tracks");
        }
    },

    StartPeriodicAudioUnlock: function() {
        if (window._vivoxUnlockInterval) {
            clearInterval(window._vivoxUnlockInterval);
            window._vivoxUnlockInterval = null;
        }

        window._vivoxUnlockInterval = setInterval(function() {
            var found = 0;
            var playing = 0;

            document.querySelectorAll('audio').forEach(function(audioObj) {
                if (!audioObj || !audioObj.srcObject) return;

                // تجاهل الـ tracks اللي خلصت
                var tracks = audioObj.srcObject.getAudioTracks();
                var allEnded = tracks.length > 0 && tracks.every(function(t) {
                    return t.readyState === 'ended';
                });
                if (allEnded) return;

                found++;

                // فعّل الـ tracks
                tracks.forEach(function(t) { t.enabled = true; });

                if (!audioObj.paused && !audioObj.muted) {
                    playing++;
                    return;
                }

                audioObj.muted = false;
                audioObj.volume = 1.0;
                var p = audioObj.play();
                if (p !== undefined) p.catch(function(e) {
                    console.warn("[WebAudio] Periodic retry failed:", e.message);
                });
            });

            if (found > 0) {
                console.log("[WebAudio] 🔄 found=" + found + " playing=" + playing);
            }

            if (found > 0 && playing === found) {
                clearInterval(window._vivoxUnlockInterval);
                window._vivoxUnlockInterval = null;
                console.log("[WebAudio] ✅ All unlocked - interval stopped");
            }

        }, 800);

        console.log("[WebAudio] 🔄 Periodic unlock started");
    },

    StopPeriodicAudioUnlock: function() {
        if (window._vivoxUnlockInterval) {
            clearInterval(window._vivoxUnlockInterval);
            window._vivoxUnlockInterval = null;
            console.log("[WebAudio] 🛑 Periodic unlock stopped");
        }
    }
});
