using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhysicsRangeExtender
{
    /// <summary>
    ///     Code from https://github.com/Gedas-S/PQSBS Thanks to Gedas for this!
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TerrainExtender : MonoBehaviour
    {
        private const int VesLoad = 13;
        private bool _crashDamage;
        private bool _joints;
        private bool _loading = true;
        private int _reset = 111;
        private int _stage;
        private Vessel _tvel;
        private IEnumerator<Vessel> _vesEnume;

        private double initHorizonDistance = 0;
        private double initmaxDetailDistance = 0;
        private double initminDetailDistance = 0;

        private void ExtendTerrainDistance()
        {
            try
            {
                if (FlightGlobals.VesselsLoaded.Count > 1 &&
                    FlightGlobals.VesselsLoaded.Count(x => x.LandedOrSplashed) >= 1)
                {

                    var pqs = FlightGlobals.currentMainBody.pqsController;

                    pqs.horizonDistance = PreSettings.GlobalRange * 1000f * 1.15;
                    pqs.maxDetailDistance = PreSettings.GlobalRange * 1000f * 1.15;
                    pqs.minDetailDistance = PreSettings.GlobalRange * 1000f * 1.15;

                    pqs.visRadSeaLevelValue = 200;
                    pqs.collapseSeaLevelValue = 200;

                    if (!_loading) return;

                    initHorizonDistance = pqs.horizonDistance;
                    initmaxDetailDistance = pqs.maxDetailDistance;
                    initminDetailDistance = pqs.minDetailDistance;

                    using (var v = FlightGlobals.VesselsLoaded.GetEnumerator())
                    {
                        while (v.MoveNext())
                        {
                            if (v.Current == null) continue;
                            if (!SortaLanded(v.Current)) return;
                            switch (_stage)
                            {
                                case 0:
                                    Debug.Log("ExtendTerrainDistance stage0");
                                    v.Current?.SetWorldVelocity(v.Current.gravityForPos * -4 * Time.fixedDeltaTime);
                                    break;
                                    Debug.Log("ExtendTerrainDistance stage1");
                                case 1:
                                    v.Current?.SetWorldVelocity(v.Current.gravityForPos * -2 * Time.fixedDeltaTime);
                                    break;
                                case 4:
                                    v.Current?.SetWorldVelocity(v.Current.velocityD / 2);
                                    break;
                                default:
                                    v.Current?.SetWorldVelocity(Vector3d.zero);
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    var pqs = FlightGlobals.currentMainBody.pqsController;

                    pqs.horizonDistance = initHorizonDistance;
                    pqs.maxDetailDistance = initmaxDetailDistance;
                    pqs.minDetailDistance = initminDetailDistance;
                }
            }
            catch (Exception)
            {
                Debug.Log("[PhysicsRangeExtender]: Exception extending terrain, updating initial values");        
            }
        }
       


        void FixedUpdate() => Apply();
        void LateUpdate() => Apply();

        private void Update()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (!PreSettings.ModEnabled) return;
            if (!PreSettings.TerrainExtenderEnabled) return;

            if (executeTerrainExtender)
            {
                ResetParameters();
                executeTerrainExtender = false;
            }
            ExtendTerrainDistance();
            EaseLoadingForExtendedRange();
        }

        private void Apply()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (!PreSettings.ModEnabled) return;
            if (!PreSettings.TerrainExtenderEnabled) return;

            ExtendTerrainDistance();
        }

        private void ResetParameters()
        {
            this._loading = true;
            this._reset = 111;
            this._stage = 0;
            _crashDamage = CheatOptions.NoCrashDamage;
            _joints = CheatOptions.UnbreakableJoints;
            CheatOptions.NoCrashDamage = true;
            CheatOptions.UnbreakableJoints = true;

        }

        private void EaseLoadingForExtendedRange()
        {
            if (!_loading) return;

            if (!FlightGlobals.currentMainBody.pqsController.isBuildingMaps)
                --_reset;
            if (_reset > 0) return;
            _reset = VesLoad;

            switch (_stage)
            {
                case 0:
                    _vesEnume = FlightGlobals.VesselsLoaded.ToList().GetEnumerator();
                    _tvel = FlightGlobals.ActiveVessel;
                    ++_stage;
                    break;
                case 1:
                    if (_vesEnume.Current != null)
                        _vesEnume.Current.OnFlyByWire -= Thratlarasat;
                    if (_vesEnume.MoveNext())
                    {
                        if (SortaLanded(_vesEnume.Current))
                            FlightGlobals.ForceSetActiveVessel(_vesEnume.Current);
                           _vesEnume.Current.mainBody.pqsController.StartUpSphere();
                        PhysicsRangeExtender.VesselToFreeze.Remove(_vesEnume.Current);
                        _vesEnume.Current.OnFlyByWire += Thratlarasat;
                    }
                    else
                    {
                        _vesEnume.Dispose();
                        ++_stage;
                        FlightGlobals.ForceSetActiveVessel(_tvel);
                    }

                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender]Extending terrain distance: entangling.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    break;
                case 2:
                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender]Extending terrain distance: condensing.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    ++_stage;
                    break;
                case 3:
                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender]Extending terrain distance: releasing energies.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    _reset = 100;
                    ++_stage;
                    break;
                case 4:
                    CheatOptions.NoCrashDamage = _crashDamage;
                    CheatOptions.UnbreakableJoints = _joints;
                    _loading = false;
                    ScreenMessages.PostScreenMessage(
                        "[PhysicsRangeExtender]Extending terrain distance: complete.", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                    break;
            }
        }
        private void Awake()
        {
            if (!PreSettings.ModEnabled) return;
            if (!PreSettings.TerrainExtenderEnabled) return;
            _crashDamage = CheatOptions.NoCrashDamage;
            _joints = CheatOptions.UnbreakableJoints;
            CheatOptions.NoCrashDamage = true;
            CheatOptions.UnbreakableJoints = true;
        }

        public static bool SortaLanded(Vessel v)
        {
            return v.mainBody.GetAltitude(v.CoM) - Math.Max(v.terrainAltitude, 0) < 100;
        }

        private void Thratlarasat(FlightCtrlState s)
        {
            s.wheelThrottle = 0;
            s.mainThrottle = 0;
        }

        public static void ExtendForNewVessel()
        {
            executeTerrainExtender = true;
        }

        public static bool executeTerrainExtender
        { get; set; }
    }
}