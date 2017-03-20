using System;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class PhysicsRangeExtender : MonoBehaviour
    {
        private readonly VesselRanges.Situation _maxSituation = new VesselRanges.Situation(181000, 190000, 200000, 172900);

        void Start()
        {
            FloatingOrigin.fetch.threshold = Mathf.Pow(200000 + 3500, 2);

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
                foreach (var v in FlightGlobals.Vessels)
                    v.vesselRanges = new VesselRanges(new VesselRanges
                    {
                        escaping = _maxSituation,
                        flying = _maxSituation,
                        landed = _maxSituation,
                        orbit = _maxSituation,
                        prelaunch = _maxSituation,
                        splashed = _maxSituation,
                        subOrbital = _maxSituation
                    });

            }
            catch (Exception e)
            {
                Debug.Log("[PhysicsEnhancer]:Failed to Load Physics Distance -" + e);
            }
        }
    }
}
