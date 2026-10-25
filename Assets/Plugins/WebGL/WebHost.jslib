// The browser side of Wickfall.Meta.WebHost. The page template defines window.WickfallHost
// (WebGLTemplates/GamePix/index.html), which forwards to the GamePix SDK when it is there.
mergeInto(LibraryManager.library, {
  WebHost_Loaded: function () {
    if (window.WickfallHost) window.WickfallHost.loaded();
  },

  WebHost_GameStart: function () {
    if (window.WickfallHost) window.WickfallHost.gameStart();
  }
});
