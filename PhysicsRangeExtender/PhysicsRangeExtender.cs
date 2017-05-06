using System;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class PhysicsRangeExtender : MonoBehaviour
    {
        private static VesselRanges _baseRanges;
        private VesselRanges.Situation _globalSituation, _landedSituation;

        private static bool _enabled = true;
        public static bool Enabled {
            get => _enabled;
            set
            {
                _enabled = value;
                UpdateRanges(_enabled);
            }

        }

        private void Start()
        {
            FloatingOrigin.fetch.threshold = Mathf.Pow(PreSettings.GlobalRange + 3500, 2);

            _globalSituation = new VesselRanges.Situation(PreSettings.GlobalRange * 1000 - 15,
                PreSettings.GlobalRange * 1000 - 10, PreSettings.GlobalRange * 1000,
                PreSettings.GlobalRange * 1000 - 20);
            _landedSituation = new VesselRanges.Situation(PreSettings.RangeForLandedVessels * 1000 - 15,
                PreSettings.RangeForLandedVessels * 1000 - 10, PreSettings.RangeForLandedVessels * 1000,
                PreSettings.RangeForLandedVessels * 1000 - 20);

            _baseRanges = new VesselRanges
            {
                escaping = _globalSituation,
                flying = _globalSituation,
                landed = _landedSituation,
                orbit = _globalSituation,
                prelaunch = _globalSituation,
                splashed = _globalSituation,
                subOrbital = _globalSituation
            };
            GameEvents.onVesselCreate.Add(ApplyPhysRange);
        }

        private void ApplyPhysRange(Vessel data)
        {
            if (Enabled)
            {
                data.vesselRanges = new VesselRanges(_baseRanges);
            }
        }


        private static void UpdateRanges(bool enabled)
        {
            try
            {
                var vesselsCount = FlightGlobals.Vessels.Count;
                for (var i = 0; i < vesselsCount; i++)
                {
                    if (enabled)
                    {
                        FlightGlobals.Vessels[i].vesselRanges = new VesselRanges(_baseRanges);
                    }
                    else
                    {
                        FlightGlobals.Vessels[i].vesselRanges = new VesselRanges();
                    }
                   
                }
            }
            catch (Exception e)
            {
                Debug.Log("[PhysicsRangeExtender]:Failed to Load Physics Distance -" + e);
            }
        }
    }
}