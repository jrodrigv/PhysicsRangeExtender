using System;
using System.Globalization;
using KSP.UI.Screens;
using UnityEngine;

// ReSharper disable NotAccessedField.Local

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Gui : MonoBehaviour
    {
        private const float WindowWidth = 250;
        private const float DraggableHeight = 40;
        private const float LeftIndent = 12;
        private const float ContentTop = 20;
        public static Gui Fetch;
        public static bool GuiEnabled;
        public static bool HasAddedButton;
        private readonly float _incrButtonWidth = 26;
        private readonly float contentWidth = WindowWidth - 2 * LeftIndent;
        private readonly float entryHeight = 20;
        private float _contentWidth;
        private bool _gameUiToggle;
        private string _guiGlobalRangeForVessels = String.Empty;

        private float _windowHeight = 250;
        private Rect _windowRect;
        private string _guiCamFixMultiplier;

        private void Awake()
        {
            if (Fetch)
                Destroy(Fetch);

            Fetch = this;
        }

        private void Start()
        {
            _windowRect = new Rect(Screen.width - WindowWidth - 40, 100, WindowWidth, _windowHeight);
            AddToolbarButton();
            GameEvents.onHideUI.Add(GameUiDisable);
            GameEvents.onShowUI.Add(GameUiEnable);
            _gameUiToggle = true;
            _guiGlobalRangeForVessels = PreSettings.GlobalRange.ToString();
            _guiCamFixMultiplier = PreSettings.CamFixMultiplier.ToString();
        }

        // ReSharper disable once InconsistentNaming
        private void OnGUI()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (GuiEnabled && _gameUiToggle)
                _windowRect = GUI.Window(320, _windowRect, GuiWindow, "");
        }

        private void GuiWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, WindowWidth, DraggableHeight));
            float line = 0;
            _contentWidth = WindowWidth - 2 * LeftIndent;

            DrawTitle();
            line++;
            if (PreSettings.ModEnabled)
            {
                DrawGlobalVesselRange(line);
                line++;
                DrawCamFixMultiplier(line);
                line++;
                DrawSaveButton(line);
                line++;
            }
            DisableMod(line);


            _windowHeight = ContentTop + line * entryHeight + entryHeight + entryHeight;
            _windowRect.height = _windowHeight;
        }

        private void DisableMod(float line)
        {
            var saveRect = new Rect(LeftIndent, ContentTop + line * entryHeight, contentWidth, entryHeight);


            if (PreSettings.ModEnabled)
            {
                if (GUI.Button(saveRect, "Disable Mod"))
                {
                    PreSettings.ModEnabled = false;
                    PhysicsRangeExtender.RestoreStockRanges();
                    PreSettings.SaveConfig();
                }
            }
            else
            {
                if (GUI.Button(saveRect, "Enable Mod"))
                {
                    PreSettings.ModEnabled = true;
                    Apply();
                    PreSettings.SaveConfig();
                }
            }
        }


        private void DrawGlobalVesselRange(float line)
        {
            var leftLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                normal = {textColor = Color.white}
            };

            GUI.Label(new Rect(LeftIndent, ContentTop + line * entryHeight, 60, entryHeight), "Global range:",
                leftLabel);
            float textFieldWidth = 42;
            var fwdFieldRect = new Rect(LeftIndent + contentWidth - textFieldWidth - 3 * _incrButtonWidth,
                  ContentTop + line * entryHeight, textFieldWidth, entryHeight);
            _guiGlobalRangeForVessels = GUI.TextField(fwdFieldRect, _guiGlobalRangeForVessels);
          
        }

        private void DrawCamFixMultiplier(float line)
        {
            var leftLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(LeftIndent, ContentTop + line * entryHeight, 60, entryHeight), "Cam fix multiplier:",
                leftLabel);
            float textFieldWidth = 42;
            var fwdFieldRect = new Rect(LeftIndent + contentWidth - textFieldWidth - 3 * _incrButtonWidth,
                ContentTop + line * entryHeight, textFieldWidth, entryHeight);

           this._guiCamFixMultiplier = GUI.TextField(fwdFieldRect, _guiCamFixMultiplier.ToString());
           
        }

        private void DrawSaveButton(float line)
        {
            var saveRect = new Rect(LeftIndent, ContentTop + line * entryHeight, contentWidth / 2, entryHeight);
            if (GUI.Button(saveRect, "Apply new range"))
                Apply();
        }

        private void Apply()
        {
            if (int.TryParse(_guiGlobalRangeForVessels, out var parseGlobalRange))
            {
                PreSettings.GlobalRange = parseGlobalRange;
                _guiGlobalRangeForVessels = PreSettings.GlobalRange.ToString();
            }

            if (float.TryParse(_guiCamFixMultiplier, out var parseCamFix))
            {
                PreSettings.CamFixMultiplier = parseCamFix;
                _guiCamFixMultiplier = PreSettings.CamFixMultiplier.ToString();
            }


            PreSettings.SaveConfig();
            PhysicsRangeExtender.UpdateRanges(true);
        }

        private void DrawTitle()
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = {textColor = Color.white}
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(0, 0, WindowWidth, 20), "Physics Range Extender", titleStyle);
        }

        private void AddToolbarButton()
        {
            if (!HasAddedButton)
            {
                Texture buttonTexture = GameDatabase.Instance.GetTexture("PhysicsRangeExtender/Textures/icon", false);
                ApplicationLauncher.Instance.AddModApplication(EnableGui, DisableGui, Dummy, Dummy, Dummy, Dummy,
                    ApplicationLauncher.AppScenes.ALWAYS, buttonTexture);
                HasAddedButton = true;
            }
        }

        private void EnableGui()
        {
            GuiEnabled = true;
            Debug.Log("[PhysicsRangeExtender]: Showing PRE GUI");
        }

        private void DisableGui()
        {
            GuiEnabled = false;
            Debug.Log("[PhysicsRangeExtender]: Hiding PRE GUI");
        }

        private void Dummy()
        {
        }

        private void GameUiEnable()
        {
            _gameUiToggle = true;
        }

        private void GameUiDisable()
        {
            _gameUiToggle = false;
        }
    }
}