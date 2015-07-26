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
        public SolarFlux mSolar;

        public void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            mVessels = new Dictionary<Vessel, RadiationTracker>();
            mSolar = new SolarFlux();

            Logging.Log("KappaRay Core loaded successfully.");
        }

        public RadiationTracker getRT(Vessel v)
        {
            if (!mVessels.ContainsKey(v))
            {
                mVessels[v] = new RadiationTracker(v);
            }
            return mVessels[v];
        }

        public void Update()
        {
            mSolar.Update();
            foreach(RadiationTracker rt in mVessels.Values)
            {
                rt.Update();
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

