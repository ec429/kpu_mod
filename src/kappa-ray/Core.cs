using System;
using System.Collections.Generic;
using UnityEngine;

namespace kapparay
{
    public class SolarFlux // Represents the current k-ray output of the Sun
    {
        public double flux
        {
            get
            {
                double extra = 0, t = Planetarium.GetUniversalTime();
                if (t - lastStorm < 3600 * 6 * 2)
                    extra = Math.Sin(Math.Pow((t - lastStorm) / (3600 * 6 * 2), 0.3) * Math.PI);
                return Math.Exp(logFlux) + extra * stormStrength;
            }
            protected set
            {
                logFlux = Math.Log(value);
            }
        }
        private double logFlux;

        private double nextStorm;
        private double lastStorm;
        private double stormStrength;

        public void Update()
        {
            if (Planetarium.GetUniversalTime() > nextStorm)
            {
                TimeWarp.SetRate(0, true); // force drop out of timewarp, they're probably going to want to do something about this
                Logging.Message("SOLAR STORM DETECTED!");
                lastStorm = nextStorm;
                stormStrength = 4.0 + Math.Pow(Core.Instance.mRandom.NextDouble(), 2) * 50.0;
                ScheduleStorm();
            }
            else if (Double.IsPositiveInfinity(nextStorm) && FlightGlobals.ready && FlightGlobals.ActiveVessel && !FlightGlobals.ActiveVessel.HoldPhysics)
            {
                ScheduleStorm();
            }
            logFlux = logFlux * 0.999 + (Core.Instance.mRandom.NextDouble() - 0.5) * 0.004;
        }

        public void OnSave(ConfigNode node)
        {
            node.AddValue("logFlux", logFlux);
            if (!Double.IsNegativeInfinity(lastStorm))
                node.AddValue("lastStorm", lastStorm);
            node.AddValue("nextStorm", nextStorm);
            node.AddValue("stormStrength", stormStrength);
        }

        public void OnLoad(ConfigNode node)
        {
            if (node.HasValue("logFlux"))
                Double.TryParse(node.GetValue("logFlux"), out logFlux);
            lastStorm = Double.NegativeInfinity;
            if (node.HasValue("lastStorm"))
                Double.TryParse(node.GetValue("lastStorm"), out lastStorm);
            if (node.HasValue("nextStorm"))
                Double.TryParse(node.GetValue("nextStorm"), out nextStorm);
            else
                ScheduleStorm();
            if (node.HasValue("stormStrength"))
                Double.TryParse(node.GetValue("stormStrength"), out stormStrength);
        }

        private void ScheduleStorm()
        {
            try
            {
                nextStorm = Planetarium.GetUniversalTime() + 3600 * 6 * 2 + Core.Instance.mRandom.Next(3600 * 6 * 98);
                Logging.Log("Next storm scheduled for " + KSPUtil.PrintDate((int)nextStorm, true, true));
            }
            catch (NullReferenceException exc)
            {
                Logging.Log("Failed to schedule storm: " + exc.Message + "\n" + exc.StackTrace);
            }
        }

        public SolarFlux()
        {
            flux = 1.0;
            lastStorm = Double.NegativeInfinity;
            nextStorm = Double.PositiveInfinity;
        }
    }

    public class Core : MonoBehaviour
    {
        public static Core Instance { get; protected set; }
        public static Vessel EmptyVessel;

        private Dictionary<Vessel,RadiationTracker> mVessels;
        private Dictionary<string,KerbalTracker> mKerbals;
        public SolarFlux mSolar;
        public System.Random mRandom;
        private ApplicationLauncherButton mButton;
        private UI.FluxWindow mFluxWindow;
        private UI.RosterWindow mRosterWindow;

        public Core()
        {
            if (Core.Instance != null)
            {
                Destroy(this);
                return;
            }

            Core.Instance = this;
            mVessels = new Dictionary<Vessel, RadiationTracker>();
            mKerbals = new Dictionary<string, KerbalTracker>();
            mSolar = new SolarFlux();
            mRandom = new System.Random();
            mFluxWindow = new UI.FluxWindow();
            mRosterWindow = new UI.RosterWindow();
            EmptyVessel = new Vessel();
            EmptyVessel.vesselName = "Unassigned";
        }

        protected void Awake()
        {
            try
            {
                GameEvents.onGUIApplicationLauncherReady.Add(this.OnGuiAppLauncherReady);
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
            }
            Logging.Log("ActionMenu was created.", false);
        }

        protected void Start()
        {
            if (mButton == null)
            {
                OnGuiAppLauncherReady();
            }
        }

        private void OnGUI()
        {
            GUI.depth = 0;
            Action windows = delegate { };
            foreach (var window in UI.AbstractWindow.Windows.Values)
            {
                windows += window.Draw;
            }
            windows.Invoke();
        }

        private void OnGuiAppLauncherReady()
        {
            try
            {
                mButton = ApplicationLauncher.Instance.AddModApplication(
                OnChange,
                OnChange,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.ALWAYS,
                GameDatabase.Instance.GetTexture("kappa-ray/Textures/toolbar_icon", false));
                GameEvents.onGameSceneLoadRequested.Add(this.OnSceneChange);
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
            }
        }

        private void OnChange()
        {
            mFluxWindow.toggleWindow();
        }

        private void OnSceneChange(GameScenes s)
        {
            if (s != GameScenes.FLIGHT)
            {
                mFluxWindow.Hide();
                mRosterWindow.Hide();
            }
        }

        public void ShowRoster()
        {
            mRosterWindow.Show();
        }

        public Dictionary<Vessel,List<KerbalTracker>> TrackedKerbals()
        {
            Dictionary<Vessel,List<KerbalTracker>> rv = new Dictionary<Vessel, List<KerbalTracker>>();
            rv.Add(EmptyVessel, new List<KerbalTracker>());
            foreach(KerbalTracker kt in mKerbals.Values)
            {
                rv[EmptyVessel].Add(kt);
            }
            foreach(Vessel v in mVessels.Keys)
            {
                rv.Add(v, new List<KerbalTracker>());
                foreach(ProtoCrewMember cm in v.GetVesselCrew())
                {
                    KerbalTracker kt = getKT(cm);
                    if (rv[EmptyVessel].Contains(kt))
                        rv[EmptyVessel].Remove(kt);
                    rv[v].Add(kt);
                }
            }
            return rv;
        }

        public RadiationTracker getRT(Vessel v)
        {
            if (v == null)
                return null;
            if (!mVessels.ContainsKey(v))
                mVessels[v] = new RadiationTracker(v);
            return mVessels[v];
        }

        public KerbalTracker getKT(ProtoCrewMember cm)
        {
            return getKTbyName(cm.name);
        }

        public KerbalTracker getKTbyName(string name)
        {
            if (!mKerbals.ContainsKey(name))
                mKerbals[name] = new KerbalTracker(name);
            return mKerbals[name];
        }

        public void ForgetVessel(Vessel v)
        {
            mVessels.Remove(v);
        }

        public void ForgetKerbal(KerbalTracker kt)
        {
            mKerbals.Remove(kt.name);
        }

        public void Update()
        {
            mSolar.Update();
            foreach(Vessel v in FlightGlobals.Vessels) // ensure every vessel has a RadiationTracker
            {
                getRT(v);
            }
            foreach(RadiationTracker rt in mVessels.Values)
            {
                rt.Update();
            }
            foreach(KerbalTracker kt in mKerbals.Values)
            {
                if (kt.Update())
                    ForgetKerbal(kt);
            }
        }

        public void Save(ConfigNode node)
        {
            ConfigNode solarNode = new ConfigNode("solar");
            mSolar.OnSave(solarNode);
            node.AddNode(solarNode);
            foreach(KerbalTracker kt in mKerbals.Values)
            {
                ConfigNode ktNode = new ConfigNode("kerbalTracker");
                kt.OnSave(ktNode);
                node.AddNode(ktNode);
            }
        }

        public void Load(ConfigNode node)
        {
            ConfigNode solarNode = node.GetNode("solar");
            if (solarNode != null)
                mSolar.OnLoad(solarNode);
            mVessels.Clear();
            mKerbals.Clear();
            foreach (ConfigNode ktNode in node.GetNodes("kerbalTracker"))
            {
                if (ktNode.HasValue("name"))
                {
                    string name = ktNode.GetValue("name");
                    KerbalTracker kt = new KerbalTracker(name);
                    kt.OnLoad(ktNode);
                    mKerbals.Add(name, kt);
                }
            }
            Logging.Log("KappaRay Core loaded successfully.");
        }

        public void OnDestroy()
        {
            try
            {
                GameEvents.onGUIApplicationLauncherReady.Remove(this.OnGuiAppLauncherReady);
                if (mButton != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(mButton);
                }
            }
            catch (Exception ex)
            {
                Logging.Exception(ex);
            }
            Logging.Log("ActionMenu was destroyed.", false);
            Instance = null;
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CoreFlight : Core {}
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class CoreSC : Core {}
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class CoreTracking : Core {}

    [KSPScenario(ScenarioCreationOptions.AddToNewGames, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
    public class ScenarioKappaRay : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            Core.Instance.Save(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            Core.Instance.Load(node);
        }
    }

    [KSPAddon (KSPAddon.Startup.MainMenu, true)]
    public class AddKappaRayToEVA : MonoBehaviour
    {
        private void EvaAddPartModule(AvailablePart part, string module)
        {
            try
            {
                ConfigNode mn = new ConfigNode("MODULE");
                mn.AddValue("name", module);
                part.partPrefab.AddModule(mn);
                Logging.Log("The expected exception did not happen when adding " + module + " to " + part.name + "!");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Object reference not set"))
                {
                    Logging.Log("kappa-ray: added " + module + " to " + part.name, false);
                }
                else
                {
                    Logging.Log("Unexpected error while adding " + module + " to " + part.name + ": " + ex.Message + "\n" + ex.StackTrace, false);
                }
            }
        }

        public void Start ()
        {
            EvaAddPartModule(PartLoader.getPartInfoByName("kerbalEVA"), "ModuleKappaRayEVA");
            EvaAddPartModule(PartLoader.getPartInfoByName("kerbalEVAfemale"), "ModuleKappaRayEVA");
        }
    }
}

