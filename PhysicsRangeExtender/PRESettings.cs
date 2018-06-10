using System;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class PreSettings : MonoBehaviour
    {
        public static string SettingsConfigUrl = "GameData/PhysicsRangeExtender/settings.cfg";

        public static int RangeForLandedVessels { get; set; }
        public static int GlobalRange { get; set; }
        public static bool ExtendedTerrain { get; set; }

        void Awake()
        {
            LoadConfig();
        }

        public static void LoadConfig()
        {
            try
            {
                Debug.Log("[PhysicsRangeExtender]: Loading settings.cfg ==");

                ConfigNode fileNode = ConfigNode.Load(SettingsConfigUrl);
                if (!fileNode.HasNode("PreSettings")) return;

                ConfigNode settings = fileNode.GetNode("PreSettings");
                RangeForLandedVessels = int.Parse(settings.GetValue("RangeForLandedVessels"));
                GlobalRange = int.Parse(settings.GetValue("GlobalRange"));
                ExtendedTerrain = bool.Parse(settings.GetValue("ExtendedTerrain"));
            }
            catch (Exception ex)
            {
                Debug.Log("[PhysicsRangeExtender]: Failed to load settings config:" + ex.Message);
            }
        }

        public static void SaveConfig()
        {
            try
            {
                Debug.Log("[PhysicsRangeExtender]: Saving settings.cfg ==");
                ConfigNode fileNode = ConfigNode.Load(SettingsConfigUrl);
                if (!fileNode.HasNode("PreSettings")) return;
                ConfigNode settings = fileNode.GetNode("PreSettings");

                settings.SetValue("RangeForLandedVessels", RangeForLandedVessels);
                settings.SetValue("GlobalRange", GlobalRange);
                settings.SetValue("ExtendedTerrain", ExtendedTerrain);
                fileNode.Save(SettingsConfigUrl);
            }
            catch (Exception ex)
            {
                Debug.Log("[PhysicsRangeExtender]: Failed to save settings config:" + ex.Message); throw;
            }
        }


    }
}
