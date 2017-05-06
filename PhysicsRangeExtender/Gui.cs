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
        private float _contentWidth;
        private bool _gameUiToggle;
        private bool _guiEnabled;
        private bool _hasAddedButton;
        private float _windowHeight = 250;
        private Rect _windowRect;


        private readonly float entryHeight = 20;


        void Start()
        {
            _windowRect = new Rect(Screen.width - WindowWidth - 40, 0, WindowWidth, _windowHeight);
            AddToolbarButton();
            GameEvents.onHideUI.Add(GameUiDisable);
            GameEvents.onShowUI.Add(GameUiEnable);
        }

        void OnGUI()
        {
            if (_guiEnabled && _gameUiToggle)
                _windowRect = GUI.Window(320, _windowRect, GuiWindow, "");
        }

        private void GuiWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, WindowWidth, DraggableHeight));
            float line = 1;
            _contentWidth = WindowWidth - 2 * LeftIndent;

            DrawTitle();
            line++;
            DrawModStateButton(line);


            _windowHeight = ContentTop + line * entryHeight + entryHeight + entryHeight;
            _windowRect.height = _windowHeight;
        }

        private void DrawModStateButton(float line)
        {
            line++;
            var saveRect = new Rect(LeftIndent, ContentTop + line * entryHeight, _contentWidth / 2, entryHeight);
            if (PhysicsRangeExtender.Enabled)
            {
                if (GUI.Button(saveRect, "Disable mod"))
                    PhysicsRangeExtender.Enabled = false;
            }
            else
            {
                if (GUI.Button(saveRect, "Enable mod"))
                    PhysicsRangeExtender.Enabled = true;
            }
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
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(0, 20, WindowWidth, 40), "Physics Range Extender", titleStyle);
        }

        private void AddToolbarButton()
        {
            if (!_hasAddedButton)
            {
                Texture buttonTexture = GameDatabase.Instance.GetTexture("PhysicsRangeExtender/Textures/icon", false);
                ApplicationLauncher.Instance.AddModApplication(EnableGui, DisableGui, Dummy, Dummy, Dummy, Dummy,
                    ApplicationLauncher.AppScenes.FLIGHT, buttonTexture);
                _hasAddedButton = true;
            }
        }

        private void EnableGui()
        {
            _guiEnabled = true;
            Debug.Log("Showing PRE GUI");
        }

        private void DisableGui()
        {
            _guiEnabled = false;
            Debug.Log("Hiding PRE GUI");
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