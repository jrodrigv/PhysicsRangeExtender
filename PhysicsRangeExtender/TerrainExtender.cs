using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace PhysicsRangeExtender
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TerrainExtender : MonoBehaviour
    {
        public enum LandedVesselsStates
        {
            NotFocused,
            Focusing,
            Focused,
            Lifted,
            Landed
        }

        private static bool _crashDamage;
        private static bool _joints;
        private bool _initialLoading;
        private bool _loading;
        private Vessel _tvel;

        public static List<VesselLandedState> vesselsLandedToLoad { get; set; } = new List<VesselLandedState>();

        public static void UpdateSphere()
        {

            var pqs = FlightGlobals.currentMainBody.pqsController;


            Debug.Log($" pqs.horizonDistance ={ pqs.horizonDistance}");
            Debug.Log($" pqs.maxDetailDistance ={   pqs.maxDetailDistance}");
            Debug.Log($" pqs.minDetailDistance ={   pqs.minDetailDistance}");

            Debug.Log($"  pqs.detailAltitudeMax ={   pqs.detailAltitudeMax}");
            Debug.Log($"  pqs.collapseAltitudeMax ={    pqs.collapseAltitudeMax}");
            Debug.Log($"  pqs.detailSeaLevelQuads ={     pqs.detailSeaLevelQuads}");
            Debug.Log($"  pqs.detailAltitudeQuads ={    pqs.detailAltitudeQuads}");
            Debug.Log($"  pqs.maxQuadLenghtsPerFrame  ={    pqs.maxQuadLenghtsPerFrame }");
            Debug.Log($"  pqs.visRadSeaLevelValue ={   pqs.visRadSeaLevelValue}");
            Debug.Log($"  pqs.collapseSeaLevelValue ={   pqs.collapseSeaLevelValue}");


            //pqs.maxDetailDistance = double.MaxValue;
            //pqs.minDetailDistance = double.MaxValue;
            pqs.detailAltitudeMax = Mathf.Max(PreSettings.GlobalRange * 1000f, 100000);
            pqs.visRadAltitudeMax = Mathf.Max(PreSettings.GlobalRange * 1000f, 100000);
            pqs.collapseAltitudeMax = Mathf.Max(PreSettings.GlobalRange * 1000f, 100000) * 10;
            pqs.detailSeaLevelQuads = 0;
            pqs.detailAltitudeQuads = 0;
            pqs.maxQuadLenghtsPerFrame = 0.03f;
            pqs.visRadSeaLevelValue = 200;
            pqs.collapseSeaLevelValue = 200;
            pqs.StartUpSphere();
        }

        private void FixedUpdate()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (!PreSettings.ModEnabled) return;
            if (!PreSettings.TerrainExtenderEnabled) return;
            if (!FlightGlobals.ready) return;


            ExtendTerrainForLandedVessels();
        }

        private void ExtendTerrainForLandedVessels()
        {
            if (FlightGlobals.currentMainBody.pqsController.isBuildingMaps) return;

            InitialFetch();

            
            vesselsLandedToLoad.RemoveAll(x => x.Vessel == null);


            if (vesselsLandedToLoad.Count == 0) return;

            if (!_loading)
            {
                _loading = true;
                ActivateNoCrashDamage();

                _tvel = FlightGlobals.ActiveVessel;
            }

            foreach (var currentVesselData in vesselsLandedToLoad)
            {
                var currentVessel = currentVesselData.Vessel;

                if (currentVessel == null) continue;
                if (!SortaLanded(currentVessel)) continue;

                switch (currentVesselData.LandedState)
                {
                    case LandedVesselsStates.NotFocused:

                       
                        if (currentVesselData.Vessel != _tvel)
                        {
                            if (vesselsLandedToLoad.Any(x =>
                                x.Vessel != currentVessel && x.LandedState == LandedVesselsStates.Focusing)) return;

                            if (InternalCamera.Instance.isActiveAndEnabled)
                            {
                                InternalCamera.Instance.DisableCamera();
                                CameraManager.Instance.SetCameraFlight();
                            }

                            FlightGlobals.ForceSetActiveVessel(currentVessel);
                            CameraManager.Instance.SetCameraFlight();

                            //UpdateSphere();
                            currentVesselData.LandedState = LandedVesselsStates.Focusing;
                            currentVesselData.TimeOfState = Time.time;
                        }
                        else
                        {
                            currentVesselData.LandedState = LandedVesselsStates.Focused;
                        }
                        currentVessel.SetWorldVelocity(currentVessel.gravityForPos * -8 * Time.fixedDeltaTime);
                        break;
                    case LandedVesselsStates.Focusing:

                        if (Time.time - currentVesselData.TimeOfState > 2)
                        {
                            currentVesselData.LandedState = LandedVesselsStates.Focused;

                            foreach (var vesselLandedState in vesselsLandedToLoad.Where(x =>x.LandedState == LandedVesselsStates.NotFocused && Vector3.Distance(currentVessel.CoM, x.Vessel.CoM) < 2500))
                            {
                                vesselLandedState.LandedState = LandedVesselsStates.Focused;
                            }
                        }
                        currentVessel.SetWorldVelocity(currentVessel.gravityForPos * -8 * Time.fixedDeltaTime);
                        break;
                    case LandedVesselsStates.Focused:

                        if (currentVessel.altitude - currentVesselData.InitialAltitude >= 3d)
                        {
                            currentVesselData.LandedState = LandedVesselsStates.Lifted;
                        }
                        else
                        {
                            currentVessel.SetWorldVelocity(currentVessel.gravityForPos * -8 * Time.fixedDeltaTime);
                            currentVessel.UpdateLandedSplashed();
                        }
                            
                        break;
                    case LandedVesselsStates.Lifted:

                        if (!currentVessel.Landed)
                        {
                            currentVessel.SetWorldVelocity(currentVessel.gravityForPos.normalized* 20.0f  *Time.fixedDeltaTime);
                            currentVessel.UpdateLandedSplashed();
                        }
                        else
                        {
                            currentVessel.SetWorldVelocity(Vector3.zero);
                            currentVesselData.LandedState = LandedVesselsStates.Landed;
                        }

                        break;
                    case LandedVesselsStates.Landed:
                        currentVessel.SetWorldVelocity(Vector3.zero);
                        break;
                  
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            vesselsLandedToLoad.RemoveAll(x => x.LandedState == LandedVesselsStates.Landed);

            if (FlightGlobals.ActiveVessel != _tvel && vesselsLandedToLoad.All(x => x.LandedState != LandedVesselsStates.NotFocused && x.LandedState != LandedVesselsStates.Focusing))
            {
                FlightGlobals.ForceSetActiveVessel(_tvel);
                CameraManager.Instance.SetCameraFlight();
            }

            if (vesselsLandedToLoad.Count == 0)
            {
                DeactivateNoCrashDamage();
                _loading = false;
            }
        }

        private void InitialFetch()
        {
            if (_initialLoading && FlightGlobals.VesselsLoaded.Count >= 1)
            {
                foreach (var vessel in FlightGlobals.VesselsLoaded)
                {
                    if (vessel.isActiveVessel) continue;

                    if ((vessel.Landed || SortaLanded(vessel)) &&
                        vessel.vesselType != VesselType.Debris)
                        vesselsLandedToLoad.Add(new VesselLandedState
                        {
                            InitialAltitude = vessel.altitude,
                            LandedState = LandedVesselsStates.NotFocused,
                            Vessel = vessel
                        });
                }

                _initialLoading = false;
            }
        }

        private void Update()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (!PreSettings.ModEnabled) return;
            if (!PreSettings.TerrainExtenderEnabled) return;

            ShowMessageTerrainStatus();
        }

        private void ShowMessageTerrainStatus()
        {
            if (vesselsLandedToLoad.Count == 0) return;


            var overrallStatus = LandedVesselsStates.NotFocused;

            if (vesselsLandedToLoad.Any(x => x.LandedState == LandedVesselsStates.NotFocused))
                overrallStatus = LandedVesselsStates.NotFocused;
            else if (vesselsLandedToLoad.Any(x => x.LandedState == LandedVesselsStates.Focusing))
                overrallStatus = LandedVesselsStates.Focusing;
            else if (vesselsLandedToLoad.Any(x => x.LandedState == LandedVesselsStates.Focused))
                overrallStatus = LandedVesselsStates.Focused;
            else if (vesselsLandedToLoad.Any(x => x.LandedState == LandedVesselsStates.Lifted))
                overrallStatus = LandedVesselsStates.Lifted;
            else if (vesselsLandedToLoad.Any(x => x.LandedState == LandedVesselsStates.Landed))
                overrallStatus = LandedVesselsStates.Landed;

            switch (overrallStatus)
            {
                case LandedVesselsStates.NotFocused:
                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender]Extending terrain: focusing landed vessels.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    break;
                case LandedVesselsStates.Focusing:
                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender]Extending terrain: focusing landed vessels.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    break;
                case LandedVesselsStates.Focused:
                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender]Extending terrain: lifting vessels.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    break;
                case LandedVesselsStates.Lifted:
                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender]Extending terrain: landing vessels.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    break;
                case LandedVesselsStates.Landed:
                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender]Extending terrain: switching to previous vessel.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void DeactivateNoCrashDamage()
        {
            CheatOptions.NoCrashDamage = _crashDamage;
            CheatOptions.UnbreakableJoints = _joints;
        }


        private void Start()
        {
            if (!PreSettings.ModEnabled) return;
            if (!PreSettings.TerrainExtenderEnabled) return;

            _initialLoading = true;
        }
   

       

        public static void ActivateNoCrashDamage()
        {
            _crashDamage = CheatOptions.NoCrashDamage;
            _joints = CheatOptions.UnbreakableJoints;
            CheatOptions.NoCrashDamage = true;
            CheatOptions.UnbreakableJoints = true;
        }

        public static bool SortaLanded(Vessel v)
        {
            if (v.Splashed) return false;

            return v.mainBody.GetAltitude(v.CoM) - Math.Max(v.terrainAltitude, 0) < 100;
        }

        public class VesselLandedState
        {
            public Vessel Vessel { get; set; }
            public LandedVesselsStates LandedState { get; set; }
            public double InitialAltitude { get; set; }

            public double TimeOfState { get; set; }
        }
    }
}