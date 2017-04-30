using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class PRESettings : MonoBehaviour
    {
        public static string settingsConfigURL = "GameData/PhysicsRangeExtender/settings.cfg";

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

                ConfigNode fileNode = ConfigNode.Load(settingsConfigURL);
                if (!fileNode.HasNode("PRESettings")) return;

                ConfigNode settings = fileNode.GetNode("PRESettings");

                RangeForLandedVessels = int.Parse(settings.GetValue("RangeForLandedVessels"));
                GlobalRange = int.Parse(settings.GetValue("GlobalRange"));
                
            }
            catch (Exception ex)
            {
                Debug.Log("== PhysicsRangeExtender : Failed to load settings config==");
            }
        }
    }
}
