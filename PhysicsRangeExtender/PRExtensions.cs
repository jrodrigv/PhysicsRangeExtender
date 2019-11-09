using System.Collections.Generic;
using System;
using UniLinq;
using UnityEngine;

namespace PhysicsRangeExtender
{
    public static class PRExtensions
    {
        public static bool _wasEnabled = false;

        public static void PreOn(string _modName)
        {
            if (!PreSettings.ModEnabled && _wasEnabled)
            {
                Debug.Log("[Physic Range Extender] === Being turned on by " + _modName);

                PreSettings.ModEnabled = true;
                Gui.Fetch.Apply();
                PreSettings.SaveConfig();
            }
        }

        public static void PreOff(string _modName)
        {
            if (PreSettings.ModEnabled)
            {
                _wasEnabled = true;
                Debug.Log("[Physic Range Extender] === Being turned off by " + _modName);
                PreSettings.ModEnabled = false;
                PhysicsRangeExtender.RestoreStockRanges();
                PreSettings.SaveConfig();
            }
        }
    }
}