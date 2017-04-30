using System;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class PreSettings : MonoBehaviour
    {
        public static string SettingsConfigUrl = "GameData/PhysicsRangeExtender/settings.cfg";

        public static int RangeForLandedVessels = 2000;
        public static int GlobalRange = 2000;

        void Awake()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            try
            {
                Debug.Log("== PhysicsRangeExtender: Loading settings.cfg ==");

                ConfigNode fileNode = ConfigNode.Load(SettingsConfigUrl);
                if (!fileNode.HasNode("PreSettings")) return;

                ConfigNode settings = fileNode.GetNode("PreSettings");

                RangeForLandedVessels = int.Parse(settings.GetValue("RangeForLandedVessels"));
                GlobalRange = int.Parse(settings.GetValue("GlobalRange"));
                
            }
            catch (Exception ex)
            {
                Debug.Log("== PhysicsRangeExtender : Failed to load settings config:" + ex.Message);
            }
        }
    }
}
