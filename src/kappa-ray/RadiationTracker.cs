using System;
using System.Collections.Generic;
using UnityEngine;

namespace kapparay
{
    public class RadiationTracker : MonoBehaviour
    {
        private Vessel mVessel;

        private ScreenMessage mV, mS, mG;

        private Vector3 mSaveCOM;

        public RadiationTracker (Vessel v)
        {
            mVessel = v;
        }

        public enum RadiationSource { VanAllen, Solar, Galactic };

        public void Update()
        {
            if (FlightDriver.Pause) return;
            if (!mVessel.isActiveVessel) return; /* Can't figure out how to handle background vessels reliably */
            double solarFlux = Core.Instance.mSolar.flux * mVessel.solarFlux / 1400.0; // scale solarFlux to kerbin==1
            double altitude = mVessel.altitude;
            double atm = mVessel.atmDensity;
            bool directSolar = mVessel.directSunlight;
            CelestialBody planetID = mVessel.mainBody;
            double magnetic, magcap;
            double vanAllen, solar, galactic;
            switch(planetID.flightGlobalsIndex) /* This completely relies on the indices not being changed.  Mods that add planets will screw this up */
            {
                case 0: // Sun
                    vanAllen = 1000.0 * Math.Exp(-altitude/100e6) * Core.Instance.mSolar.flux;
                    solar = 1.0;
                    galactic = Math.Max(1.0 - vanAllen, 0.0);
                    break;
                case 1: // Kerbin
                    // TODO latitude dependence for Van Allen belts
                    magnetic = Math.Exp(-altitude / 900e3);
                    magcap = (magnetic - magnetic * magnetic) * 4.0;
                    vanAllen = Math.Max(magcap * magcap - atm * 100.0, 0.0);
                    solar = Math.Max(1.0 - magnetic * 1.12 - atm * 100.0, 0.0);
                    galactic = Math.Exp(-20e6 / altitude);
                    break;
                default: // TODO handle the other bodies.  For now, they have no magnetospheres or atmospheric absorption
                    vanAllen = 0.0;
                    solar = 1.0;
                    galactic = 1.0;
                    break;
                /*
                kapparay: 0: Sun
                kapparay: 1: Kerbin
                kapparay: 2: Mun
                kapparay: 3: Minmus
                kapparay: 4: Moho
                kapparay: 5: Eve
                kapparay: 6: Duna
                kapparay: 7: Ike
                kapparay: 8: Jool
                kapparay: 9: Laythe
                kapparay: 10: Vall
                kapparay: 11: Bop
                kapparay: 12: Tylo
                kapparay: 13: Gilly
                kapparay: 14: Pol
                kapparay: 15: Dres
                kapparay: 16: Eeloo
                */
            }
            if (!mVessel.HoldPhysics)
                mSaveCOM = mVessel.CurrentCoM;
            Irradiate(vanAllen, RadiationSource.VanAllen);
            if (directSolar)
            {
                Irradiate(solar * solarFlux, RadiationSource.Solar);
            }
            else if (mS != null)
            {
                mS.message = String.Empty;
            }
            Irradiate(galactic * 0.05, RadiationSource.Galactic);
            if (mVessel.isActiveVessel)
            {
                if (mV == null)
                    mV = new ScreenMessage(String.Empty, 4.0f, ScreenMessageStyle.UPPER_LEFT);
                mV.message = String.Format("kray: Van Allen: {0:G3}", vanAllen);
                ScreenMessages.PostScreenMessage(mV, true);
                if (directSolar) {
                    if (mS == null)
                        mS = new ScreenMessage(String.Empty, 4.0f, ScreenMessageStyle.UPPER_LEFT);
                    mS.message = String.Format("kray: Solar: {0:G3}", solar * solarFlux);
                    ScreenMessages.PostScreenMessage(mS, true);
                }
                if (mG == null)
                    mG = new ScreenMessage(String.Empty, 4.0f, ScreenMessageStyle.UPPER_LEFT);
                mG.message = String.Format("kray: Galactic: {0:G3}", galactic * 0.05);
                ScreenMessages.PostScreenMessage(mG, true);
            }
        }

        private Vector3 randomVector(float length)
        {
            Vector3 v = new Vector3((float)(Core.Instance.mRandom.NextDouble() - 0.5),
                                    (float)(Core.Instance.mRandom.NextDouble() - 0.5),
                                    (float)(Core.Instance.mRandom.NextDouble() - 0.5));
            if (v.magnitude < 1e-6) // very unlikely
                v = Vector3.up;
            Vector3 mag = new Vector3(length, length, length);
            v.Normalize();
            v.Scale(mag);
            return v;
        }

        private int RaycastSorter(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }

        public void Irradiate(double strength, RadiationSource source)
        {
            Vector3 aimPt = mSaveCOM + randomVector(10.0f);
            Vector3 aimDir = randomVector(1e4f);
            strength *= TimeWarp.CurrentRate;
            int count = 1;
            if (strength >= 0.1)
                count = (int)Math.Ceiling(Core.Instance.mRandom.NextDouble() * 10.0 * strength);
            else if (Core.Instance.mRandom.NextDouble() > strength * 10.0)
                return; // count=0
            double energy = 0;
            switch (source)
            {
                case RadiationSource.VanAllen: // low-energy
                    energy = 10.0 + Core.Instance.mRandom.NextDouble() * 150.0;
                    break;
                case RadiationSource.Solar: // medium-energy
                    energy = 120.0 + Core.Instance.mRandom.NextDouble() * 300.0;
                    aimDir = Sun.Instance.sunDirection;
                    break;
                case RadiationSource.Galactic: // high-energy
                    energy = (1.0 - 4.0 * Math.Log(Core.Instance.mRandom.NextDouble())) * 300.0;
                    break;
            }

            List<RaycastHit> hits = new List<RaycastHit>(Physics.RaycastAll(aimPt - aimDir, aimDir, 2e4f));
            hits.Sort(RaycastSorter);

            foreach(RaycastHit rh in hits)
            {
                Part p = rh.transform.gameObject.GetComponent<Part>();
                if (p != null)
                {
                    bool hasModule = false;
                    foreach(Modules.ModuleKappaRayHandler h in p.FindModulesImplementing<Modules.ModuleKappaRayHandler>())
                    {
                        hasModule = true;
                        if (count == 0) break;
                        count = h.OnRadiation(energy, count);
                    }
                    if (count == 0) break;
                    if (!hasModule)
                    {
                        // TODO fuel tanks should have an absorption proportional to their fill level
                        int absorbs = Modules.ModuleKappaRayAbsorber.absorbCount(count, 0.2);
                        p.AddThermalFlux(absorbs * energy / 1e3);
                        Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}, {3:D} absorbed", p.partInfo.title, count, energy, absorbs), false);
                        count -= absorbs;
                    }
                }
            }
        }
    }
}

