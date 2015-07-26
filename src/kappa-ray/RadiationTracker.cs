using System;
using UnityEngine;

namespace kapparay
{
    public class RadiationTracker : MonoBehaviour
    {
        public Vessel vessel;

        private ScreenMessage mV, mS, mG;

        public RadiationTracker (Vessel v)
        {
            vessel = v;
            mV = new ScreenMessage(String.Empty, 4f, ScreenMessageStyle.UPPER_LEFT);
            mS = new ScreenMessage(String.Empty, 4f, ScreenMessageStyle.UPPER_LEFT);
            mG = new ScreenMessage(String.Empty, 4f, ScreenMessageStyle.UPPER_LEFT);
        }

        public enum RadiationSource { VanAllen, Solar, Galactic };

        public void Update()
        {
            double solarFlux = Core.Instance.mSolar.flux * vessel.solarFlux / 1400.0; // scale solarFlux to kerbin==1
            double altitude = vessel.altitude;
            double atm = vessel.atmDensity;
            bool directSolar = vessel.directSunlight;
            CelestialBody planetID = vessel.mainBody;
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
                    magnetic = Math.Exp(-altitude / 900e3);
                    magcap = (magnetic - magnetic * magnetic) * 4.0;
                    vanAllen = Math.Max(magcap * magcap - atm * 100.0, 0.0);
                    solar = Math.Max(1.0 - magnetic * 1.12 - atm * 100.0, 0.0);
                    galactic = Math.Max(1.0 - magnetic * 1.24 - atm * 100.0, 0.0);
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
            mV.message = String.Format("kray: Van Allen: {0:G}", vanAllen);
            ScreenMessages.PostScreenMessage(mV, true);
            Irradiate(vanAllen, RadiationSource.VanAllen);
            if (directSolar)
            {
                Irradiate(solar * solarFlux, RadiationSource.Solar);
                mS.message = String.Format("kray: Solar: {0:G}", solar * solarFlux);
                ScreenMessages.PostScreenMessage(mS, true);
            }
            else
            {
                mS.message = String.Empty;
            }
            Irradiate(galactic * 0.05, RadiationSource.Galactic);
            mG.message = String.Format("kray: Galactic: {0:G}", galactic * 0.05);
            ScreenMessages.PostScreenMessage(mG, true);
        }

        public void Irradiate(double strength, RadiationSource source)
        {
            // Vector3d sunDirection = Sun.Instance.sunDirection;

            // For now, just bathe all parts in ambient radiation, that can't even be shielded against by other parts
            foreach(Part p in vessel.Parts)
            {
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
                        break;
                    case RadiationSource.Galactic: // high-energy
                        energy = (1.0 - 4.0 * Math.Log(Core.Instance.mRandom.NextDouble())) * 300.0;
                        break;
                }
                // XXX If a part has multiple handlers, what order do they get called in?
                foreach(Modules.ModuleKappaRayHandler h in p.FindModulesImplementing<Modules.ModuleKappaRayHandler>())
                {
                    if (count == 0) break;
                    count = h.OnRadiation(energy, count);
                }
            }
        }
    }
}

