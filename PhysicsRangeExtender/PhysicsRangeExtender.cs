using System;
using System.Linq;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PhysicsRangeExtender : MonoBehaviour
    {
        private static VesselRanges _baseRanges;
        private static VesselRanges.Situation _globalSituation;

        private static bool _enabled = true;
        private static bool _forceRanges;
        private static bool _unloadDueToReferenceFrameApplied;

        private static float _initialClippingPlane = 0.21f;


        public static bool ForceRanges
        {
            get => _forceRanges;
            set
            {
                _forceRanges = value;
                ApplyRangesToVessels(_enabled);
            }
        }

        public static bool FlickeringFix { get; set; } = true;

        void Start()
        {
            UpdateRanges();
            
            GameEvents.onVesselCreate.Add(ApplyPhysRange);
            GameEvents.onVesselLoaded.Add(ApplyPhysRange);
            GameEvents.onVesselSwitching.Add(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Add(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Add(ApplyPhysRange);
        }

        void OnDestroy()
        {
            GameEvents.onVesselCreate.Remove(ApplyPhysRange);
            GameEvents.onVesselLoaded.Remove(ApplyPhysRange);
            GameEvents.onVesselSwitching.Remove(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Remove(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Remove(ApplyPhysRange);
        }


        private void ApplyPhysRange(Vessel data0, Vessel data1)
        {
            ApplyRangesToVessels();
        }

        private void ApplyPhysRange(Vessel data)
        {

            ApplyRangesToVessels();
        }


        void Update()
        {
            UpdateNearClipPlane();
        }

        private void UpdateNearClipPlane()
        {
            if (FlickeringFix &&  FlightGlobals.VesselsLoaded.Count > 1 && FlightGlobals.VesselsLoaded.Count(x => x.Landed) >= 1)
            {
                //var maxdistanceBetweenActiveVesselAndLoadedVessels = CalculateDistanceToFurthestVessel();

                var distanceMultiplier =
                    _initialClippingPlane * (FlightGlobals.ActiveVessel.transform.position.sqrMagnitude / (2500f * 2500f));

                FlightCamera.fetch.mainCamera.nearClipPlane = Mathf.Clamp(distanceMultiplier,_initialClippingPlane, _initialClippingPlane * 50f);

                FlightGlobals.ActiveVessel.Parts.Select(x =>
                    x.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate);
            }
            else
            {
                FlightCamera.fetch.mainCamera.nearClipPlane = _initialClippingPlane;
            }
        }

        void LateUpdate() => UpdateNearClipPlane();
        void FixedUpdate()
        {
            UpdateNearClipPlane();
            if (!ForceRanges)
            {
                AvoidReferenceFrameChangeIssues();
            }
        }

        private void AvoidReferenceFrameChangeIssues()
        {
            if (!ShouldLandedVesselsBeLoaded())
            {
                if (!_unloadDueToReferenceFrameApplied)
                {
                      UnloadLandedVessels();
                    _unloadDueToReferenceFrameApplied = true;
                }
            }
            else if(_unloadDueToReferenceFrameApplied)
            {
                UpdateRanges();
                _unloadDueToReferenceFrameApplied = false;
            }
        }

        /// <summary>
        /// This method will avoid landed vessels to be destroyed due to changes on the referencial frame (inertial vs rotation) when the active vessel is going suborbital
        /// </summary>
        /// <returns> if landed vessel should be loaded</returns>
        private static bool ShouldLandedVesselsBeLoaded()
        {
            var safetyMargin = 0.90f;

            if (FlightGlobals.ActiveVessel == null ||
                FlightGlobals.ActiveVessel.LandedOrSplashed ||
                FlightGlobals.ActiveVessel.orbit == null ||
                FlightGlobals.ActiveVessel.orbit.referenceBody == null)

            {
                return true;
            }

            var altitudeAtPos =
                (double) FlightGlobals.getAltitudeAtPos(FlightGlobals.ActiveVessel.transform.position,
                    FlightGlobals.ActiveVessel.orbit.referenceBody);

            if ((altitudeAtPos / FlightGlobals.ActiveVessel.orbit.referenceBody.inverseRotThresholdAltitude) >
                safetyMargin)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// This method will reduce the load/unload distances using a closer range to avoid issues.
        /// </summary>
        private void UnloadLandedVessels()
        {
            var vesselsCount = FlightGlobals.Vessels.Count;
            ScreenMessages.PostScreenMessage(
                "[PhysicsRangeExtender] Unloading landed vessels during active orbital fly.", 3f, ScreenMessageStyle.UPPER_CENTER);
            for (var i = 0; i < vesselsCount; i++)
            {
                if (FlightGlobals.Vessels[i].LandedOrSplashed)
                {

                   var safeSituation = new VesselRanges.Situation(
                        load: FlightGlobals.ActiveVessel.orbit.referenceBody.inverseRotThresholdAltitude * 0.90f,
                        unload: FlightGlobals.ActiveVessel.orbit.referenceBody.inverseRotThresholdAltitude * 0.95f,
                        pack: PreSettings.GlobalRange * 1000 * 1.10f,
                        unpack: PreSettings.GlobalRange * 1000 * 0.99f);

                    var newRanges = new VesselRanges
                    {
                        escaping = _globalSituation,
                        flying = _globalSituation,
                        landed = safeSituation,
                        orbit = _globalSituation,
                        prelaunch = safeSituation,
                        splashed = safeSituation,
                        subOrbital = _globalSituation
                    };

                    FlightGlobals.Vessels[i].vesselRanges = newRanges;
                }
            }
        }

        public static void UpdateRanges(bool updatingFromUi = false)
        {
            Debug.Log("[PhysicsRangeExtender]:  Updating ranges");
            FloatingOrigin.fetch.threshold = Mathf.Pow(PreSettings.GlobalRange * 1000 * 1.20f, 2);

           _globalSituation = new VesselRanges.Situation(
                load: PreSettings.GlobalRange * 1000,
                unload: PreSettings.GlobalRange * 1000 * 1.05f, 
                pack: PreSettings.GlobalRange * 1000 * 1.10f,
                unpack: PreSettings.GlobalRange * 1000 * 0.99f);
          
            _baseRanges = new VesselRanges
            {
                escaping = _globalSituation,
                flying = _globalSituation,
                landed = _globalSituation,
                orbit = _globalSituation,
                prelaunch = _globalSituation,
                splashed = _globalSituation,
                subOrbital = _globalSituation
            };
            ApplyRangesToVessels(updatingFromUi);
        }

        private static void ApplyRangesToVessels( bool updatingFromUi = false)
        {
            try
            {
                var vesselsCount = FlightGlobals.Vessels.Count;

                for (var i = 0; i < vesselsCount; i++)
                {
                    if (!ForceRanges)
                    {
                        // check to avoid landed vessels to be destroyec when the active vessel is sub-orbital
                        if (FlightGlobals.Vessels[i].LandedOrSplashed && !ShouldLandedVesselsBeLoaded())
                        {
                            ScreenMessages.PostScreenMessage(
                                "[PhysicsRangeExtender] Landed vessels will not be loaded during active orbital fly.", 3f, ScreenMessageStyle.UPPER_CENTER);
                            continue;
                        }
                        // 
                        if (VesselOrbitingWhileUpdatingRangeFromUi(updatingFromUi, FlightGlobals.Vessels[i]))
                        {
                            ScreenMessages.PostScreenMessage(
                                "[PhysicsRangeExtender]: Please reload the game to apply the new range to all vessels.", 3f, ScreenMessageStyle.UPPER_CENTER);
                            continue;
                        } 
                    }

                    FlightGlobals.Vessels[i].vesselRanges = new VesselRanges(_baseRanges);     

                }
            }
            catch (Exception e)
            {
                Debug.Log("[PhysicsRangeExtender]: Failed to Load Physics Distance -" + e);
            }
        }

        /// <summary>
        /// This method will avoid de-orbiting unloaded vessels when a user is extending the range using the UI and orbiting vessels are getting loaded.
        /// </summary>
        /// <param name="updatingFromUi"></param>
        /// <param name="vessel"></param>
        /// <returns></returns>
        private static bool VesselOrbitingWhileUpdatingRangeFromUi( bool updatingFromUi, Vessel vessel)
        {
            return !vessel.isActiveVessel && updatingFromUi && !vessel.LandedOrSplashed;
        }
    }
}