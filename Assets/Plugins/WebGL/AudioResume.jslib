mergeInto(LibraryManager.library, {
  WebGL_ResumeAudioContext: function () {
    try {
      if (typeof Module !== 'undefined') {
        var ctx = Module.audioContext || Module.webaudioContext;
        if (ctx && ctx.state === 'suspended') ctx.resume();
      }

      if (!window.__unity_ac) window.__unity_ac = new (window.AudioContext || window.webkitAudioContext)();
      if (window.__unity_ac.state === 'suspended') window.__unity_ac.resume();

      console.log("[WEB] AudioContext resume requested");
    } catch (e) {
      console.log("[WEB] ResumeAudioContext error:", e);
    }
  }
});