using System;
using System.Collections.Generic;
using UnityEngine;

namespace kapparay
{
    public class Core : MonoBehaviour
    {
        public static Core Instance { get; protected set; }
        private Dictionary<Vessel,RadiationTracker> mVessels;

        public void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            mVessels = new Dictionary<Vessel, RadiationTracker>();

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

