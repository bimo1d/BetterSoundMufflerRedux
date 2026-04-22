using System;
using System.Collections;
using System.Reflection;
using SpaceWarp2.UI.API.Appbar;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterSoundMufflerRedux
{
    internal static class BetterSoundMufflerUI
    {
        private const string ButtonId = "BTN-BetterSoundMufflerRedux";
        private const string ToggleTypeName = "KSP.UI.Binding.UIValue_WriteBool_Toggle";
        private const string ModTitleOverride = "Better Sound Muffler Redux";

        private static BetterSoundMufflerConfig _config;
        private static object _game;
        private static bool _appbarRegistered;
        private static Sprite _icon;
        private static GameObject _button;
        private static Component _buttonToggle;
        private static MethodInfo _buttonToggleSetValue;
        private static bool _buttonStateKnown;
        private static bool _buttonState;

        internal static void Initialize(BetterSoundMufflerConfig config)
        {
            _config = config;
        }

        internal static void Update(object game)
        {
            _game = game;
            AttachButtonHandler();
        }

        internal static void RegisterAppbarButton()
        {
            if (_appbarRegistered) return;

            _icon = CreateIcon();
            Appbar.RegisterAppButton(BetterSoundMufflerLoc.Appbar, ButtonId, _icon, OnAppbarButton);
            _appbarRegistered = true;
        }

        internal static void SetButtonState(bool enabled)
        {
            GameObject button = FindButton();
            if (button == null) return;
            if (_buttonStateKnown && _buttonState == enabled) return;

            Component toggle = FindButtonToggle(button);
            if (toggle == null || _buttonToggleSetValue == null) return;

            _buttonToggleSetValue.Invoke(toggle, new object[] { enabled });
            _buttonState = enabled;
            _buttonStateKnown = true;
        }

        internal static void OpenSettings()
        {
            object settingsManager = _game == null ? null : GetProperty(_game, "SettingsMenuManager");
            if (settingsManager == null) return;

            object menu = FindSettingsMenu(settingsManager, ModTitleOverride)
                ?? FindSettingsMenu(settingsManager, BetterSoundMufflerLoc.Title);
            if (menu == null) return;

            InvokeSetVisible(settingsManager);
            InvokeShowSubMenu(settingsManager, menu);
        }

        private static void OnAppbarButton(bool enabled)
        {
            if (_config == null || _config.Enabled == null) return;

            _buttonStateKnown = false;
            SetButtonState(_config.Enabled.Value);
        }

        private static void ToggleEnabled()
        {
            if (_config == null || _config.Enabled == null) return;

            bool enabled = !_config.Enabled.Value;
            _config.Enabled.Value = enabled;
            _config.Save();
            SetButtonState(enabled);
        }

        private static void AttachButtonHandler()
        {
            GameObject button = FindButton();
            if (button == null || button.GetComponent<AppbarButtonClickHandler>() != null) return;

            button.AddComponent<AppbarButtonClickHandler>();
        }

        private static GameObject FindButton()
        {
            if (_button != null) return _button;

            _button = GameObject.Find(ButtonId);
            _buttonToggle = null;
            _buttonToggleSetValue = null;
            _buttonStateKnown = false;
            return _button;
        }

        private static Component FindButtonToggle(GameObject button)
        {
            if (_buttonToggle != null) return _buttonToggle;

            Component[] components = button.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.GetType().FullName != ToggleTypeName) continue;

                _buttonToggle = component;
                _buttonToggleSetValue = component.GetType().GetMethod("SetValue", BindingFlags.Instance | BindingFlags.Public);
                return _buttonToggle;
            }

            return null;
        }

        private static object FindSettingsMenu(object settingsManager, string titleLocalizationKey)
        {
            return FindSettingsMenuInField(settingsManager, "_mainMenuSubMenus", titleLocalizationKey)
                ?? FindSettingsMenuInField(settingsManager, "_pauseMenuSubMenus", titleLocalizationKey)
                ?? FindSettingsMenuInField(settingsManager, "_preregisteredMenus", titleLocalizationKey);
        }

        private static object FindSettingsMenuInField(object settingsManager, string fieldName, string titleLocalizationKey)
        {
            FieldInfo field = settingsManager.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return null;

            IEnumerable list = field.GetValue(settingsManager) as IEnumerable;
            if (list == null) return null;

            foreach (object item in list)
            {
                FieldInfo item1 = item.GetType().GetField("Item1");
                object menu = item1 == null ? item : item1.GetValue(item);
                if (menu == null) continue;

                PropertyInfo title = menu.GetType().GetProperty("TitleLocalizationKey", BindingFlags.Instance | BindingFlags.Public);
                if (title != null && string.Equals(title.GetValue(menu, null) as string, titleLocalizationKey, StringComparison.Ordinal))
                {
                    return menu;
                }
            }

            return null;
        }

        private static void InvokeSetVisible(object settingsManager)
        {
            MethodInfo setVisible = settingsManager.GetType().GetMethod("SetVisible", BindingFlags.Instance | BindingFlags.Public);
            if (setVisible == null) return;

            ParameterInfo[] parameters = setVisible.GetParameters();
            if (parameters.Length == 2)
            {
                setVisible.Invoke(settingsManager, new object[] { true, false });
            }
            else if (parameters.Length == 1)
            {
                setVisible.Invoke(settingsManager, new object[] { true });
            }
        }

        private static void InvokeShowSubMenu(object settingsManager, object menu)
        {
            MethodInfo showSubMenu = settingsManager.GetType().GetMethod("ShowSubMenu", BindingFlags.Instance | BindingFlags.Public);
            if (showSubMenu == null) return;

            showSubMenu.Invoke(settingsManager, new object[] { menu });
        }

        private static object GetProperty(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null) return null;

            return property.GetValue(instance, null);
        }

        private static Sprite CreateIcon()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            Color body = new Color(0.75f, 0.95f, 1.0f, 1.0f);
            Color accent = new Color(0.25f, 0.85f, 0.55f, 1.0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            DrawLine(texture, 7, 21, 11, 14, body);
            DrawLine(texture, 11, 14, 15, 21, body);
            DrawLine(texture, 17, 21, 17, 11, body);
            DrawLine(texture, 17, 11, 23, 17, body);
            DrawLine(texture, 17, 21, 23, 15, accent);
            DrawLine(texture, 4, 8, 27, 8, accent);
            DrawLine(texture, 6, 25, 25, 25, accent);
            texture.Apply(false, true);

            return Appbar.GetAppBarIconFromTexture(texture);
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                texture.SetPixel(x0, y0, color);
                if (x0 == x1 && y0 == y1) return;

                int e2 = 2 * error;
                if (e2 >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private sealed class AppbarButtonClickHandler : MonoBehaviour, IPointerClickHandler
        {
            public void OnPointerClick(PointerEventData eventData)
            {
                if (eventData == null) return;

                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    ToggleEnabled();
                    return;
                }

                if (eventData.button == PointerEventData.InputButton.Left) OpenSettings();
            }
        }
    }
}
