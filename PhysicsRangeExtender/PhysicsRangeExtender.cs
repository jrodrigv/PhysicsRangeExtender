using System;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class PhysicsRangeExtender : MonoBehaviour
    {
        private static VesselRanges _baseRanges;
        private static VesselRanges.Situation _globalSituation;
        private static VesselRanges.Situation _landedSituation;

        private static bool _enabled = true;

        public static bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                ApplyRangesToVessels(_enabled);
            }
        }

        private void Start()
        {
            UpdateRanges();

            
            GameEvents.onVesselCreate.Add(ApplyPhysRange);
            GameEvents.onVesselSwitchingToUnloaded.Add(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Add(ApplyPhysRange);
            GameEvents.onVesselLoaded.Add(ApplyPhysRange);
            GameEvents.onVesselSwitching.Add(ApplyPhysRange);
            GameEvents.onVesselGoOnRails.Add(ApplyPhysRange);
        }

        void OnDestroy()
        {
            GameEvents.onVesselCreate.Remove(ApplyPhysRange);
            GameEvents.onVesselSwitchingToUnloaded.Remove(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Remove(ApplyPhysRange);
            GameEvents.onVesselLoaded.Remove(ApplyPhysRange);
            GameEvents.onVesselSwitching.Remove(ApplyPhysRange);
            GameEvents.onVesselGoOnRails.Remove(ApplyPhysRange);
        }

        private void ApplyPhysRange(Vessel data0, Vessel data1)
        {
            ApplyRangesToVessels(Enabled);
        }

        private void ApplyPhysRange(Vessel data)
        {
            ApplyRangesToVessels(Enabled);
        }

        public static void UpdateRanges()
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
            ApplyRangesToVessels(_enabled);
        }

        private static void ApplyRangesToVessels(bool enabled)
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