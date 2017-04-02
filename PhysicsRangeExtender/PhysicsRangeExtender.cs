using System;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class PhysicsRangeExtender : MonoBehaviour
    {
        private const int RangeInKm = 2000;
        private readonly VesselRanges.Situation _maxSituation = new VesselRanges.Situation(RangeInKm * 1000 - 15, RangeInKm * 1000 - 10, RangeInKm*1000, RangeInKm * 1000 - 20);

        void Start()
        {
            FloatingOrigin.fetch.threshold = Mathf.Pow(RangeInKm * 1000 + 3500, 2);

            GameEvents.onVesselSwitching.Add(ApplyPhysRange);
            GameEvents.onVesselCreate.Add(ApplyPhysRange);
            GameEvents.onVesselGoOnRails.Add(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Add(ApplyPhysRange);
            GameEvents.onVesselLoaded.Add(ApplyPhysRange);
            GameEvents.onVesselSwitchingToUnloaded.Add(ApplyPhysRange);

            ApplyPhysRange();

        }

        private void ApplyPhysRange(Vessel data0, Vessel data1)
        {
            ApplyPhysRange();
        }

        private void ApplyPhysRange(Vessel v)
        {
            ApplyPhysRange();
        }

        public void ApplyPhysRange()
        {
            try
            {
                int vesselsCount = FlightGlobals.Vessels.Count;
                for (int i = 0; i < vesselsCount; i++)
                {
                    FlightGlobals.Vessels[i].vesselRanges.escaping = _maxSituation;
                    FlightGlobals.Vessels[i].vesselRanges.flying = _maxSituation;
                    FlightGlobals.Vessels[i].vesselRanges.landed = _maxSituation;
                    FlightGlobals.Vessels[i].vesselRanges.orbit = _maxSituation;
                    FlightGlobals.Vessels[i].vesselRanges.prelaunch = _maxSituation;
                    FlightGlobals.Vessels[i].vesselRanges.splashed = _maxSituation;
                    FlightGlobals.Vessels[i].vesselRanges.subOrbital = _maxSituation;
                }
            }
            catch (Exception e)
            {
                Debug.Log("[PhysicsRangeExtender]:Failed to Load Physics Distance -" + e);
            }
        }
    }
}
