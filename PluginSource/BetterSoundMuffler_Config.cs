using ReduxLib.Configuration;

namespace BetterSoundMufflerRedux
{
    internal sealed class BetterSoundMufflerConfig
    {
        private IConfigFile _configFile;

        internal ConfigValue<bool> Enabled;
        internal ConfigValue<int> MuffleAmount;
        internal ConfigValue<bool> DebugLogging;

        internal void Initialize(IConfigFile config)
        {
            _configFile = config;

            Enabled = new ConfigValue<bool>(config.Bind(BetterSoundMufflerLoc.Main, BetterSoundMufflerLoc.Enabled, true, BetterSoundMufflerLoc.EnabledTip));
            MuffleAmount = new ConfigValue<int>(config.Bind(BetterSoundMufflerLoc.Main, BetterSoundMufflerLoc.MuffleAmount, 75, BetterSoundMufflerLoc.MuffleAmountTip, new ListConstraint<int>(25, 50, 75, 100)));
            DebugLogging = new ConfigValue<bool>(config.Bind(BetterSoundMufflerLoc.Advanced, BetterSoundMufflerLoc.DebugLogging, false, BetterSoundMufflerLoc.DebugLoggingTip));

            Enabled.RegisterCallback(OnEnabledChanged);
            MuffleAmount.RegisterCallback(OnMuffleAmountChanged);
            NormalizeMuffleAmount();
        }

        internal void Save()
        {
            if (_configFile != null)
            {
                _configFile.Save();
            }
        }

        internal int GetMuffleAmount()
        {
            if (MuffleAmount == null)
            {
                return 75;
            }

            return ClampToStep(MuffleAmount.Value);
        }

        private static void OnEnabledChanged(bool oldValue, bool newValue)
        {
            BetterSoundMufflerUI.SetButtonState(newValue);
        }

        private void OnMuffleAmountChanged(int oldValue, int newValue)
        {
            int value = ClampToStep(newValue);
            if (value != newValue && MuffleAmount != null)
            {
                MuffleAmount.Value = value;
                Save();
            }
        }

        private void NormalizeMuffleAmount()
        {
            if (MuffleAmount == null)
            {
                return;
            }

            int value = ClampToStep(MuffleAmount.Value);
            if (value == MuffleAmount.Value)
            {
                return;
            }

            MuffleAmount.Value = value;
            Save();
        }

        private static int ClampToStep(int value)
        {
            if (value <= 37)
            {
                return 25;
            }

            if (value <= 62)
            {
                return 50;
            }

            if (value <= 87)
            {
                return 75;
            }

            return 100;
        }
    }
}
