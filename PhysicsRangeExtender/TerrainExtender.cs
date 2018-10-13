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
        private static PqsData _originalPqsData;

        private void ExtendTerrainDistance()
        {
            try
            {
                var pqs = FlightGlobals.currentMainBody.pqsController;
                pqs.horizonDistance = PreSettings.GlobalRange * 1000f;
                pqs.maxDetailDistance = PreSettings.GlobalRange * 1000f;
                pqs.minDetailDistance = PreSettings.GlobalRange * 1000f;
                pqs.visRadSeaLevelValue = 200;
                pqs.collapseSeaLevelValue = 200;

                if (!_loading) return;

                using (var v = FlightGlobals.VesselsLoaded.GetEnumerator())
                {
                    while (v.MoveNext())
                    {
                        if (v.Current == null) continue;
                        if (!SortaLanded(v.Current)) return;
                        switch (_stage)
                        {
                            case 0:
                                v.Current?.SetWorldVelocity(v.Current.gravityForPos * -4 * Time.fixedDeltaTime);
                                break;
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
            catch (Exception)
            {
                // ignored
            }
        }
       


        void FixedUpdate() => Apply();
        void LateUpdate() => Apply();

        private void Update()
        {
            if (!PreSettings.ConfigLoaded) return;

            if (!PreSettings.TerrainExtender) return;

            ExtendTerrainDistance();
            EaseLoadingForExtendedRange();
        }

        private void Apply()
        {
            if (!PreSettings.ConfigLoaded) return;
            if (!PreSettings.TerrainExtender) return;

            ExtendTerrainDistance();
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
                    Debug.LogError($"Black Spell entangling {_vesEnume.Current?.vesselName}");
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
            if (!PreSettings.TerrainExtender) return;

            _crashDamage = CheatOptions.NoCrashDamage;
            _joints = CheatOptions.UnbreakableJoints;
            CheatOptions.NoCrashDamage = true;
            CheatOptions.UnbreakableJoints = true;
        }

        private bool SortaLanded(Vessel v)
        {
            return v.mainBody.GetAltitude(v.CoM) - Math.Max(v.terrainAltitude, 0) < 100;
        }

        private void Thratlarasat(FlightCtrlState s)
        {
            s.wheelThrottle = 0;
            s.mainThrottle = 0;
        }
    }
}