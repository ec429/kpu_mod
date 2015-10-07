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

        public bool radiationEnabled = true;

        private Dictionary<Vessel,RadiationTracker> mVessels;
        private Dictionary<string,KerbalTracker> mKerbals;
        public SolarFlux mSolar;
        public System.Random mRandom;
        private ApplicationLauncherButton mButton;
        private UI.FluxWindow mFluxWindow;
        private UI.RosterWindow mRosterWindow;
        public Dictionary<string, RadiationEnvironment> va;
        public Dictionary<string, RadiationEnvironment> so;
        public Dictionary<string, RadiationEnvironment> ga;
        public Dictionary<string, double> resAbsCe;

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
            va = new Dictionary<string, RadiationEnvironment>();
            so = new Dictionary<string, RadiationEnvironment>();
            ga = new Dictionary<string, RadiationEnvironment>();
            resAbsCe = new Dictionary<string, double>();
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

            foreach (ConfigNode cn in GameDatabase.Instance.GetConfigNodes("KappaRayEnvironment"))
            {
                if (!cn.HasValue("name"))
                {
                    Logging.Log("No name in a KappaRayEnvironment node");
                    continue;
                }
                string name = cn.GetValue("name");
                if (cn.HasValue("vanAllen"))
                {
                    string v = cn.GetValue("vanAllen");
                    va.Add(name, RadiationEnvironment.Parse(v));
                }
                if (cn.HasValue("directSolar"))
                {
                    string v = cn.GetValue("directSolar");
                    so.Add(name, RadiationEnvironment.Parse(v));
                }
                if (cn.HasValue("galactic"))
                {
                    string v = cn.GetValue("galactic");
                    ga.Add(name, RadiationEnvironment.Parse(v));
                }
            }

            foreach (ConfigNode cn in GameDatabase.Instance.GetConfigNodes("KappaRayResource"))
            {
                if (!cn.HasValue("name"))
                {
                    Logging.Log("No name in a KappaRayEnvironment node");
                    continue;
                }
                string name = cn.GetValue("name");
                double rac;
                if (cn.HasValue("absorpCoeff") && Double.TryParse(cn.GetValue("absorpCoeff"), out rac))
                {
                    resAbsCe.Add(name, rac);
                }
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

        private Dictionary<Vessel,List<KerbalTracker>> mTracked = new Dictionary<Vessel, List<KerbalTracker>>();

        public Dictionary<Vessel,List<KerbalTracker>> TrackedKerbals()
        {
            return mTracked;
        }

        private void updateTrackedKerbals()
        {
            mTracked = new Dictionary<Vessel, List<KerbalTracker>>();
            mTracked.Add(EmptyVessel, new List<KerbalTracker>());
            foreach(KerbalTracker kt in mKerbals.Values)
            {
                mTracked[EmptyVessel].Add(kt);
            }
            foreach(Vessel v in mVessels.Keys)
            {
                mTracked.Add(v, new List<KerbalTracker>());
                foreach(ProtoCrewMember cm in v.GetVesselCrew())
                {
                    KerbalTracker kt = getKT(cm);
                    if (mTracked[EmptyVessel].Contains(kt))
                        mTracked[EmptyVessel].Remove(kt);
                    mTracked[v].Add(kt);
                }
            }
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

        private double lastUpdate = 0;

        public void Update()
        {
            double t = Planetarium.GetUniversalTime();
            mSolar.Update();
            if (radiationEnabled)
            {
                foreach(Vessel v in FlightGlobals.Vessels) // ensure every vessel has a RadiationTracker
                {
                    getRT(v);
                }
                foreach(RadiationTracker rt in mVessels.Values)
                {
                    rt.Update(t - lastUpdate);
                }
                List<KerbalTracker> kerbals = new List<KerbalTracker>(mKerbals.Values);
                foreach(KerbalTracker kt in kerbals)
                {
                    if (kt.Update())
                        ForgetKerbal(kt);
                }
            }
            updateTrackedKerbals();
            lastUpdate = t;
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("radiationEnabled", radiationEnabled);
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
            radiationEnabled = true;
            if (node.HasValue("radiationEnabled"))
                Boolean.TryParse(node.GetValue("radiationEnabled"), out radiationEnabled);
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
                    Logging.Log("Loading " + name);
                    KerbalTracker kt = new KerbalTracker(name);
                    kt.OnLoad(ktNode);
                    mKerbals.Add(name, kt);
                }
                else
                {
                    Logging.Log(String.Format("No name found in kerbalTracker {0}", ktNode.ToString()));
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

