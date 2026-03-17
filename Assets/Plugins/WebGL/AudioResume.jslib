mergeInto(LibraryManager.library, {
    // 🌟 الفخ اللي بيصطاد مايك Vivox أول ما يشتغل (كودك الأصلي)
    InitWebMicInterceptor: function() {
        if (window.micInterceptorInitialized) return;
        window.micInterceptorInitialized = true;
        window.activeAudioStreams = []; // الخزنة اللي هنشيل فيها المايكات

        // 1. صيد المايك بتاعك
        var origGetUserMedia = navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices);
        navigator.mediaDevices.getUserMedia = function(constraints) {
            return origGetUserMedia(constraints).then(function(stream) {
                if (constraints && constraints.audio) {
                    window.activeAudioStreams.push(stream);
                    console.log("[WebAudio] Intercepted Vivox Audio Stream! 🎣");
                }
                return stream;
            });
        };
        console.log("[WebAudio] Mic Interceptor Installed ✅");

        // 2. 🌟 شبكة صيد الصوت اللي جاي من الناس التانية (عشان نحل مشكلتك الوحيدة)
        window.vivoxAudioElements = [];
        var originalPlay = HTMLAudioElement.prototype.play;
        HTMLAudioElement.prototype.play = function() {
            if (window.vivoxAudioElements.indexOf(this) === -1) {
                window.vivoxAudioElements.push(this);
                console.log("[WebAudio] Caught Vivox Incoming Audio! 🕸️");
            }
            return originalPlay.apply(this, arguments).catch(function(e){});
        };
    },

    // 🌟 الدالة اللي زرار (Enable Audio) بتاعك بينادي عليها
    ForceUnlockWebAudio: function() {
        if (window.vivoxAudioElements) {
            var count = 0;
            window.vivoxAudioElements.forEach(function(audioObj) {
                audioObj.muted = false;
                audioObj.volume = 1.0;
                var p = audioObj.play();
                if(p !== undefined) p.catch(function(e){});
                count++;
            });
            console.log("[WebAudio] Forced play on " + count + " streams! 🚀");
        }

        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
            if (WEBAudio.audioContext.state === 'suspended') {
                WEBAudio.audioContext.resume();
            }
        }
    },

    ResumeWebAudioContext: function() {
        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
            if (WEBAudio.audioContext.state === 'suspended') {
                WEBAudio.audioContext.resume();
                console.log("[WebAudio] AudioContext resumed via JS Bridge ✅");
            }
        }
    },

    HardMuteWebMic: function(mute) {
        if (window.activeAudioStreams && window.activeAudioStreams.length > 0) {
            var count = 0;
            window.activeAudioStreams.forEach(function(stream) {
                var tracks = stream.getAudioTracks();
                for (var i = 0; i < tracks.length; i++) {
                    tracks[i].enabled = !mute; // قفل تيار Vivox الحقيقي
                    count++;
                }
            });
            console.log("[WebAudio] Real Hard Mute set to: " + mute + " on " + count + " tracks.");
        } else {
            console.warn("[WebAudio] No active streams caught yet!");
        }
    }
});