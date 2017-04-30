using System;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class PhysicsRangeExtender : MonoBehaviour
    {
        private VesselRanges.Situation _globalSituation, _landedSituation;

        void Start()
        {
            FloatingOrigin.fetch.threshold = Mathf.Pow(PRESettings.GlobalRange + 3500, 2);

            _globalSituation = new VesselRanges.Situation(PRESettings.GlobalRange * 1000 - 15, PRESettings.GlobalRange * 1000 - 10, PRESettings.GlobalRange * 1000, PRESettings.GlobalRange * 1000 - 20);
            _landedSituation = new VesselRanges.Situation(PRESettings.RangeForLandedVessels * 1000 - 15, PRESettings.RangeForLandedVessels * 1000 - 10, PRESettings.RangeForLandedVessels * 1000, PRESettings.RangeForLandedVessels * 1000 - 20);

            GameEvents.onVesselSwitching.Add(ApplyPhysRange);
            GameEvents.onVesselCreate.Add(ApplyPhysRange);
            GameEvents.onVesselGoOnRails.Add(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Add(ApplyPhysRange);
            GameEvents.onVesselLoaded.Add(ApplyPhysRange);

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
                    FlightGlobals.Vessels[i].vesselRanges = new VesselRanges(new VesselRanges
                    {
                        escaping = _globalSituation,
                        flying = _globalSituation,
                        landed = _landedSituation,
                        orbit = _globalSituation,
                        prelaunch = _globalSituation,
                        splashed = _globalSituation,
                        subOrbital = _globalSituation
                    });
                }
            }
            catch (Exception e)
            {
                Debug.Log("[PhysicsRangeExtender]:Failed to Load Physics Distance -" + e);
            }
        }
    }
}
