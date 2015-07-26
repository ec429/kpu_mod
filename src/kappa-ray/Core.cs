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
        private System.Random mRandom;
        private double logFlux;
        public void Update()
        {
            logFlux = logFlux * 0.999 + (mRandom.NextDouble() - 0.5) * 0.004;
        }

        public SolarFlux()
        {
            flux = 1.0;
            mRandom = new System.Random();
        }
    }

    public class Core : MonoBehaviour
    {
        public static Core Instance { get; protected set; }
        private Dictionary<Vessel,RadiationTracker> mVessels;
        private Dictionary<Kerbal,KerbalTracker> mKerbals;
        public SolarFlux mSolar;
        public System.Random mRandom;

        public void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            mVessels = new Dictionary<Vessel, RadiationTracker>();
            mKerbals = new Dictionary<Kerbal, KerbalTracker>();
            mSolar = new SolarFlux();
            mRandom = new System.Random();

            Logging.Log("KappaRay Core loaded successfully.");
        }

        public RadiationTracker getRT(Vessel v)
        {
            if (!mVessels.ContainsKey(v))
                mVessels[v] = new RadiationTracker(v);
            return mVessels[v];
        }

        public KerbalTracker getKT(Kerbal k)
        {
            if (!mKerbals.ContainsKey(k))
                mKerbals[k] = new KerbalTracker(k);
            return mKerbals[k];
        }

        public void ForgetVessel(Vessel v)
        {
            mVessels.Remove(v);
        }

        public void ForgetKerbal(Kerbal k)
        {
            mKerbals.Remove(k);
        }

        public void Update()
        {
            mSolar.Update();
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
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CoreFlight : Core
    {
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class CoreTracking : Core
    {
    }
}

