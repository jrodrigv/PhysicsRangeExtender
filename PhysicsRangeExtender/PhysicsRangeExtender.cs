using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PhysicsRangeExtender : MonoBehaviour
    {
        private static VesselRanges _baseRanges;
        private static VesselRanges.Situation _globalSituation;
        private static bool _unloadDueToReferenceFrameApplied;

        private static readonly float _initialClippingPlane = 0.21f;
        private bool _isSuborbital;

        public List<Vessel> VesselToFreeze { get; set; } = new List<Vessel>();


        public double LastFlickeringTime { get; set; }

        private void Start()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (!PreSettings.ModEnabled) return;

            TerrainExtender.UpdateSphere();
            UpdateRanges();

            GameEvents.onVesselCreate.Add(ApplyPhysRange);
            GameEvents.onVesselLoaded.Add(ApplyPhysRangeOnLoad);
            GameEvents.onVesselSwitching.Add(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Add(ApplyPhysRange);
            GameEvents.onVesselSituationChange.Add(SituationChangeFixes);
        }

        private void SituationChangeFixes(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
        {
            RefreshPqsWhenApproaching(data);
        }


        private void RefreshPqsWhenApproaching(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
        {
            var curVessel = data.host;
            if (!curVessel.mainBody.isHomeWorld || !curVessel.isActiveVessel) return;

            if (data.from == Vessel.Situations.FLYING && data.to == Vessel.Situations.SUB_ORBITAL)
            {
                _isSuborbital = true;
            }
            else if (_isSuborbital && data.to == Vessel.Situations.FLYING)
            {
                _isSuborbital = false;
                Debug.Log("[PhysicsRangeExtender]: Calling StartUpSphere() to prevent missing PQ tiles");
                curVessel.mainBody.pqsController.StartUpSphere();
            }
        }

        private void ApplyPhysRangeOnLoad(Vessel data)
        {
            NewVesselIsLoaded(data);
            ApplyRangesToVessels();
        }

        private void OnDestroy()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (!PreSettings.ModEnabled) return;
            GameEvents.onVesselCreate.Remove(ApplyPhysRange);
            GameEvents.onVesselLoaded.Remove(ApplyPhysRangeOnLoad);
            GameEvents.onVesselSwitching.Remove(ApplyPhysRange);
            GameEvents.onVesselGoOffRails.Remove(ApplyPhysRange);
            GameEvents.onVesselSituationChange.Add(SituationChangeFixes);
        }


        private void NewVesselIsLoaded(Vessel vessel)
        {
            if (vessel != null && !vessel.isActiveVessel && vessel.Landed && vessel.vesselType != VesselType.Debris)
                if (TerrainExtender.vesselsLandedToLoad.All(x => x.Vessel.id != vessel.id))
                    TerrainExtender.vesselsLandedToLoad.Add(new TerrainExtender.VesselLandedState
                    {
                        Vessel = vessel, InitialAltitude = vessel.altitude,
                        LandedState = TerrainExtender.LandedVesselsStates.NotFocused
                    });
        }


        private void ApplyPhysRange(Vessel data0, Vessel data1)
        {
            CheckIfFreezeIsNeeded(data0, data1);
            ApplyRangesToVessels();
        }

        private void CheckIfFreezeIsNeeded(Vessel from, Vessel to)
        {
            if (from.Landed && to.situation >= Vessel.Situations.SUB_ORBITAL)
            {
                TerrainExtender.ActivateNoCrashDamage();
                from.SetWorldVelocity(Vector3d.zero);
                VesselToFreeze.Add(from);
                VesselToFreeze.AddRange(FlightGlobals.VesselsLoaded.Where(x => x.LandedOrSplashed));
            }
        }

        private static void ApplyPhysRange(Vessel data)
        {
            ApplyRangesToVessels();
        }


        private void Update()
        {
            if (!PreSettings.ModEnabled) return;
            UpdateNearClipPlane();
            AvoidReferenceFrameChangeIssues();
            FreezeLandedVesselWhenSwitching();
        }

        private void UpdateNearClipPlane()
        {
            if (FlightGlobals.VesselsLoaded.Count > 1 &&
                FlightGlobals.VesselsLoaded.Count(x => x.LandedOrSplashed) >= 1)
            {
                var distanceMultiplier =
                    _initialClippingPlane *
                    (FlightGlobals.ActiveVessel.transform.position.sqrMagnitude / (4000f * 4000f)) *
                    PreSettings.CamFixMultiplier;

                FlightCamera.fetch.mainCamera.nearClipPlane = Mathf.Clamp(distanceMultiplier, _initialClippingPlane,
                    _initialClippingPlane * 50f);

                FlightGlobals.ActiveVessel.Parts.ForEach(x =>
                    x.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate);

                if (Time.time - LastFlickeringTime > 60)
                {
                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender] Flickering correction is active, near camera plane is adapting.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    LastFlickeringTime = Time.time;
                }
            }
            else
            {
                FlightCamera.fetch.mainCamera.nearClipPlane = _initialClippingPlane;
            }
        }

        private void LateUpdate()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (!PreSettings.ModEnabled) return;
            UpdateNearClipPlane();
            AvoidReferenceFrameChangeIssues();
            FreezeLandedVesselWhenSwitching();
        }

        private void FixedUpdate()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (!PreSettings.ModEnabled) return;
            UpdateNearClipPlane();
            AvoidReferenceFrameChangeIssues();
            FreezeLandedVesselWhenSwitching();
        }

        private void FreezeLandedVesselWhenSwitching()
        {
            VesselToFreeze.RemoveAll(x => x == null);
            VesselToFreeze.RemoveAll(x => !x.loaded);

            if (VesselToFreeze.Count == 0) TerrainExtender.DeactivateNoCrashDamage();
            VesselToFreeze.ForEach(x => x?.SetWorldVelocity(Vector3d.zero));
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
            else if (_unloadDueToReferenceFrameApplied)
            {
                UpdateRanges();
                _unloadDueToReferenceFrameApplied = false;
            }
        }

        /// <summary>
        ///     This method will avoid landed vessels to be destroyed due to changes on the referencial frame (inertial vs
        ///     rotation) when the active vessel is going suborbital
        /// </summary>
        /// <returns> if landed vessel should be loaded</returns>
        private static bool ShouldLandedVesselsBeLoaded()
        {
            var safetyMargin = 0.90f;

            if (FlightGlobals.ActiveVessel == null ||
                FlightGlobals.ActiveVessel.LandedOrSplashed ||
                FlightGlobals.ActiveVessel.orbit == null ||
                FlightGlobals.ActiveVessel.orbit.referenceBody == null)
                return true;

            var altitudeAtPos =
                (double) FlightGlobals.getAltitudeAtPos(FlightGlobals.ActiveVessel.transform.position,
                    FlightGlobals.ActiveVessel.orbit.referenceBody);

            if (altitudeAtPos / FlightGlobals.ActiveVessel.orbit.referenceBody.inverseRotThresholdAltitude >
                safetyMargin)
                return false;
            return true;
        }

        /// <summary>
        ///     This method will reduce the load/unload distances using a closer range to avoid issues.
        /// </summary>
        private void UnloadLandedVessels()
        {
            var vesselsCount = FlightGlobals.VesselsLoaded.Count;
            ScreenMessages.PostScreenMessage(
                "[PhysicsRangeExtender] Unloading landed vessels during active orbital fly.", 3f,
                ScreenMessageStyle.UPPER_CENTER);
            for (var i = 0; i < vesselsCount; i++)
                if (FlightGlobals.VesselsLoaded[i].LandedOrSplashed)
                {
                    var safeSituation = new VesselRanges.Situation(
                        FlightGlobals.ActiveVessel.orbit.referenceBody.inverseRotThresholdAltitude * 0.90f,
                        FlightGlobals.ActiveVessel.orbit.referenceBody.inverseRotThresholdAltitude * 0.95f,
                        FlightGlobals.ActiveVessel.orbit.referenceBody.inverseRotThresholdAltitude * 1.10f,
                        FlightGlobals.ActiveVessel.orbit.referenceBody.inverseRotThresholdAltitude * 0.99f);

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

                    FlightGlobals.VesselsLoaded[i].vesselRanges = newRanges;
                }
        }

        public static void UpdateRanges(bool updatingFromUi = false)
        {
            Debug.Log("[PhysicsRangeExtender]:  Updating ranges");
            FloatingOrigin.fetch.threshold = Mathf.Pow(PreSettings.GlobalRange * 1000 * 1.20f, 2);

            if (updatingFromUi) TerrainExtender.UpdateSphere();

            _globalSituation = new VesselRanges.Situation(
                PreSettings.GlobalRange * 1000,
                PreSettings.GlobalRange * 1000 * 1.05f,
                PreSettings.GlobalRange * 1000 * 1.10f,
                PreSettings.GlobalRange * 1000 * 0.99f);

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

        private static void ApplyRangesToVessels(bool updatingFromUi = false)
        {
            if (!PreSettings.ModEnabled) return;
            try
            {
                var vesselsCount = FlightGlobals.Vessels.Count;

                for (var i = 0; i < vesselsCount; i++)
                {
                    // check to avoid landed vessels to be destroyed when the active vessel is sub-orbital
                    if (FlightGlobals.Vessels[i].LandedOrSplashed && !ShouldLandedVesselsBeLoaded()) continue;
                    // 
                    if (VesselOrbitingWhileUpdatingRangeFromUi(updatingFromUi, FlightGlobals.Vessels[i])) continue;

                    FlightGlobals.Vessels[i].vesselRanges = new VesselRanges(_baseRanges);
                }
            }
            catch (Exception e)
            {
                Debug.Log("[PhysicsRangeExtender]: Failed to Load Physics Distance -" + e);
            }
        }

        /// <summary>
        ///     This method will avoid de-orbiting unloaded vessels when a user is extending the range using the UI and orbiting
        ///     vessels are getting loaded.
        /// </summary>
        /// <param name="updatingFromUi"></param>
        /// <param name="vessel"></param>
        /// <returns></returns>
        private static bool VesselOrbitingWhileUpdatingRangeFromUi(bool updatingFromUi, Vessel vessel)
        {
            return !vessel.isActiveVessel && updatingFromUi && !vessel.LandedOrSplashed;
        }

        public static void RestoreStockRanges()
        {
            try
            {
                FlightCamera.fetch.mainCamera.nearClipPlane = _initialClippingPlane;
                var vesselsCount = FlightGlobals.Vessels.Count;

                for (var i = 0; i < vesselsCount; i++) FlightGlobals.Vessels[i].vesselRanges = new VesselRanges();
            }
            catch (Exception e)
            {
                Debug.Log("[PhysicsRangeExtender]: Failed to Load Physics Distance -" + e);
            }
        }
    }
}