using Redux.ExtraModTypes;
using HarmonyLib;

namespace BetterSoundMufflerRedux
{
    public sealed class BetterSoundMuffler : KerbalMod
    {
        private const string HarmonyId = "bimo1d.BetterSoundMufflerRedux";

        private readonly BetterSoundMufflerConfig _config = new BetterSoundMufflerConfig();
        private BetterSoundMufflerController _controller;
        private Harmony _harmony;

        public override void OnPreInitialized()
        {
            _config.Initialize(SWConfiguration);
            BetterSoundMufflerUI.Initialize(_config);
        }

        public override void OnInitialized()
        {
            _controller = new BetterSoundMufflerController(this, _config);
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(typeof(BetterSoundMuffler).Assembly);
            BetterSoundMufflerUI.RegisterAppbarButton();
        }

        public void Update()
        {
            BetterSoundMufflerUI.Update(Game);
            if (_config.Enabled != null)
            {
                BetterSoundMufflerUI.SetButtonState(_config.Enabled.Value);
            }
        }

        public void LateUpdate()
        {
            if (_controller != null)
            {
                _controller.LateUpdate();
            }
        }

        public void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.ResetAll();
                BetterSoundMufflerController.ClearActive(_controller);
            }

            if (_harmony != null)
            {
                _harmony.UnpatchAll(HarmonyId);
            }
        }
    }

    internal static class BetterSoundMufflerLoc
    {
        internal const string Title = "BetterSoundMuffler/Title";
        internal const string Appbar = "BetterSoundMuffler/AppBar";
        internal const string Main = "Main";
        internal const string Advanced = "Advanced";
        internal const string Enabled = "BetterSoundMuffler/Settings/Enabled";
        internal const string EnabledTip = "BetterSoundMuffler/Settings/EnabledTip";
        internal const string MuffleAmount = "BetterSoundMuffler/Settings/MuffleAmount";
        internal const string MuffleAmountTip = "BetterSoundMuffler/Settings/MuffleAmountTip";
        internal const string DebugLogging = "BetterSoundMuffler/Advanced/DebugLogging";
        internal const string DebugLoggingTip = "BetterSoundMuffler/Advanced/DebugLoggingTip";
    }
}
