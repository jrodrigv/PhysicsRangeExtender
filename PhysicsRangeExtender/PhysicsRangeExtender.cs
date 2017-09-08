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
        private static bool _unloadDueToReferenceFrameApplied = false;
        public static bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                ApplyRangesToVessels(_enabled);
            }
        }

        void Start()
        {
            UpdateRanges();
 
            GameEvents.onVesselCreate.Add(ApplyPhysRange);
            GameEvents.onVesselLoaded.Add(ApplyPhysRange);
            GameEvents.onVesselSwitching.Add(ApplyPhysRange);
        }

        void OnDestroy()
        {
            GameEvents.onVesselCreate.Remove(ApplyPhysRange);
            GameEvents.onVesselLoaded.Remove(ApplyPhysRange);
            GameEvents.onVesselSwitching.Remove(ApplyPhysRange);
        }


        private void ApplyPhysRange(Vessel data0, Vessel data1)
        {
            _unloadDueToReferenceFrameApplied = false;
            ApplyRangesToVessels(Enabled);
        }

        private void ApplyPhysRange(Vessel data)
        {
            ApplyRangesToVessels(Enabled);
        }

        void FixedUpdate()
        {
            AvoidReferenceFrameChangeIssues();
        }

        private void AvoidReferenceFrameChangeIssues()
        {
            var safetyMargin = 0.90f;

            if (FlightGlobals.ActiveVessel == null ||
                FlightGlobals.ActiveVessel.LandedOrSplashed ||
                FlightGlobals.ActiveVessel.orbit == null ||
                FlightGlobals.ActiveVessel.orbit.referenceBody == null)
                
            {
                return;
            }

            var altitudeAtPos =
                (double) FlightGlobals.getAltitudeAtPos(FlightGlobals.ActiveVessel.transform.position,
                    FlightGlobals.ActiveVessel.orbit.referenceBody);

            if ((altitudeAtPos / FlightGlobals.ActiveVessel.orbit.referenceBody.inverseRotThresholdAltitude) > safetyMargin)
            {
                UnloadLandedVessels();
            }

        }

        private void UnloadLandedVessels()
        {
            var vesselsCount = FlightGlobals.Vessels.Count;

            for (var i = 0; i < vesselsCount; i++)
            {
                if (FlightGlobals.Vessels[i].LandedOrSplashed)
                {
                    FlightGlobals.Vessels[i].vesselRanges = new VesselRanges();
                    FlightGlobals.Vessels[i].vesselRanges.landed.pack =
                        PreSettings.RangeForLandedVessels * 1000 * 1.15f;
                }
            }
        }

        public static void UpdateRanges()
        {
            Debug.Log("PRE: Updating ranges");
            FloatingOrigin.fetch.threshold = Mathf.Pow(PreSettings.GlobalRange * 1.20f, 2);

            _globalSituation = new VesselRanges.Situation(
                load: PreSettings.GlobalRange * 1000,
                unload: PreSettings.GlobalRange * 1000 * 1.10f, 
                pack: PreSettings.GlobalRange * 1000 * 1.15f,
                unpack: PreSettings.GlobalRange * 1000 * 1.05f);
            _landedSituation = new VesselRanges.Situation(
                load: PreSettings.RangeForLandedVessels * 1000,
                unload: PreSettings.RangeForLandedVessels * 1000 * 1.10f,
                pack: PreSettings.RangeForLandedVessels * 1000 * 1.15f,
                unpack: PreSettings.RangeForLandedVessels * 1000 * 1.05f);

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