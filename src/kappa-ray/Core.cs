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
                return Math.Exp(logFlux);
            }
            protected set
            {
                logFlux = Math.Log(value);
            }
        }
        private double logFlux;
        public void Update()
        {
            logFlux = logFlux * 0.999 + (Core.Instance.mRandom.NextDouble() - 0.5) * 0.004;
        }

        public void OnSave(ConfigNode node)
        {
            node.AddValue("logFlux", logFlux);
        }

        public void OnLoad(ConfigNode node)
        {
            if (node.HasValue("logFlux"))
                Double.TryParse(node.GetValue("logFlux"), out logFlux);
        }

        public SolarFlux()
        {
            flux = 1.0;
        }
    }

    public class Core : MonoBehaviour
    {
        public static Core Instance { get; protected set; }

        private Dictionary<Vessel,RadiationTracker> mVessels;
        private Dictionary<string,KerbalTracker> mKerbals;
        public SolarFlux mSolar;
        public System.Random mRandom;

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
        }

        public RadiationTracker getRT(Vessel v)
        {
            if (!mVessels.ContainsKey(v))
                mVessels[v] = new RadiationTracker(v);
            return mVessels[v];
        }

        public KerbalTracker getKT(Kerbal k)
        {
            string name = k.crewMemberName;
            if (!mKerbals.ContainsKey(name))
                mKerbals[name] = new KerbalTracker(name);
            return mKerbals[name];
        }

        public void ForgetVessel(Vessel v)
        {
            mVessels.Remove(v);
        }

        public void ForgetKerbal(Kerbal k)
        {
            mKerbals.Remove(k.crewMemberName);
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
                    ForgetKerbal(kt.kerbal);
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


}

