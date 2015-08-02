using System;
using System.Collections.Generic;
using UnityEngine;

namespace kapparay
{
    public class RadiationTracker : MonoBehaviour
    {
        private Vessel mVessel;

        private ScreenMessage mV, mS, mG;

        public RadiationTracker (Vessel v)
        {
            mVessel = v;
        }

        public enum RadiationSource { VanAllen, Solar, Galactic };

        // Generic models for planetary radiation
        public double vanAllenModel(double altScale)
        {
            // TODO: support off-centre or tilted magnetic fields, like Earth has
            // (thereby producing something like the South Atlantic Anomaly)
            double magLat = mVessel.latitude;
            altScale *= Math.Cos(magLat * Math.PI / 180.0 / 2.0);
            double magnetic = Math.Exp(-mVessel.altitude / altScale);
            double magcap = (magnetic - magnetic * magnetic) * 4.0;
            double vaScale = 1.0 / (1.0 + Math.Pow(Math.Sin(magLat * Math.PI / 180.0), 2.0));
            return Math.Max(vaScale * magcap * magcap - mVessel.atmDensity * 100.0, 0.0);
        }

        public double directSolarModel(double altScale)
        {
            double magnetic = Math.Exp(-mVessel.altitude / altScale);
            return Math.Max(1.0 - magnetic * 1.12 - mVessel.atmDensity * 100.0, 0.0);
        }

        public double galacticModel(double altScale)
        {
            if (mVessel.altitude < 10) return 0;
            return Math.Max(Math.Exp(-altScale / mVessel.altitude) - mVessel.atmDensity, 0.0);
        }

        public void Update()
        {
            if (FlightDriver.Pause) return;
            if (!mVessel.isActiveVessel) return; /* Can't figure out how to handle background vessels reliably */
            double solarFlux = Core.Instance.mSolar.flux * mVessel.solarFlux / 1400.0; // scale solarFlux to kerbin==1
            bool directSolar = mVessel.directSunlight;
            CelestialBody planetID = mVessel.mainBody;
            double vanAllen, solar, galactic;
            switch(planetID.flightGlobalsIndex) /* This completely relies on the indices not being changed.  Mods that add planets will screw this up */
            {
                case 0: // Sun
                    /* The Sun's equivalent of the Van Allen belt is its corona.  Being surrounded
                     * by energetic plasma tends to result in a high radiation flux.
                     * Direct solar irradiance in interplanetary space depends only on solar flux.
                     * There is also no protection from cosmic rays (except inside the corona,
                     * where you have bigger problems).
                     */
                    vanAllen = 1000.0 * Math.Exp(-mVessel.altitude/100e6) * Core.Instance.mSolar.flux;
                    solar = 1.0;
                    galactic = Math.Max(1.0 - vanAllen, 0.0);
                    break;
                case 1: // Kerbin
                    /* Kerbin's Van Allen belts peak at around 600km altitude (i.e. 1 Kerbin radius);
                     * their peak strength is used as the reference point for the flux scale.
                     * Direct solar becomes a hazard partway through these belts, none penetrating
                     * below about 105km.
                     * Cosmic rays are heavily reduced well beyond KEO, but start to become significant
                     * once approaching the Mun's orbit.
                     */
                    vanAllen = vanAllenModel(900e3);
                    solar = directSolarModel(900e3);
                    galactic = galacticModel(20e6);
                    break;
                case 2: // Mun
                    /* The Mun has no magnetosphere of its own, but Kerbin's field gives some protection
                     * from cosmic rays.
                     */
                    vanAllen = 0.0;
                    solar = 1.0;
                    galactic = 0.173; // value for Kerbin orbit at same altitude as Mun
                    break;
                case 3: // Minmus
                    /* As the Mun, but with less help from Kerbin. */
                    vanAllen = 0.0;
                    solar = 1.0;
                    galactic = 0.653; // value for Kerbin orbit at same altitude as Minmus
                    break;
                case 4: // Moho
                    /* Based on Mercury having a magnetic field about 11% of Earth's.
                     * It's likely that, for the same reasons as Mercury, Moho will have a large,
                     * iron-rich core, whose rotation will generate a decent magnetic field.
                     * The proximity to the Sun will pump up Moho's Van Allen belts, partially
                     * making up for the lower field strength.
                     */
                    vanAllen = vanAllenModel(100e3) * 0.7;
                    solar = directSolarModel(100e3);
                    galactic = galacticModel(1e6);
                    break;
                case 5: // Eve
                    /* Being large and (probably) geologically active, Eve can be presumed to have
                     * a strong magnetosphere - probably stronger than Kerbin's.  Meanwhile the
                     * greater proximity to the Sun will contribute to strong Van Allen belts.
                     * Cosmic rays won't get anywhere _near_ Eve.  Perhaps they're scared of it?
                     */
                    vanAllen = vanAllenModel(1.4e6) * 2.5;
                    solar = directSolarModel(1.4e6);
                    galactic = galacticModel(34e6);
                    break;
                case 6: // Duna
                    /* Geologically dead, Duna's magnetic field is very weak - so that even at the
                     * edge of its atmosphere, some direct solar radiation is observed.
                     */
                    vanAllen = vanAllenModel(200e3) * 0.04;
                    solar = directSolarModel(200e3);
                    galactic = galacticModel(800e3);
                    break;
                case 7: // Ike
                    /* Ike actually has a tiny magnetic field, but it gives little protection and
                     * produces no Van Allen belts to speak of. */
                    vanAllen = 0.0;
                    solar = (2.0 + directSolarModel(10e3)) / 3.0;
                    galactic = 0.778 * (4.0 + galacticModel(60e3)) / 5.0;
                    break;
                case 8: // Jool
                    /* Jool's van Allen belts are more than usually powerful thanks to the very high
                     * electromagnetic activity of the gas giant.  Beware.
                     */
                    vanAllen = vanAllenModel(12e6) * 2.0;
                    solar = directSolarModel(12e6) * 0.8 + galacticModel(50e6) * 0.2;
                    galactic = galacticModel(1e9);
                    break;
                case 9: // Laythe
                    /* Laythe is subjected to a considerable magnetic beating from Jool which produces
                     * some radiation belts.  However Jool does also help to block out other radiation.
                     */
                    vanAllen = vanAllenModel(4e3) * 0.15;
                    solar = directSolarModel(8e3) * 0.825;
                    galactic = 0.0;
                    break;
                case 10: // Vall
                    /* Vall has no magnetic field to speak of. */
                    vanAllen = 0.0;
                    solar = 0.857;
                    galactic = 0.0;
                    break;
                case 11: // Bop
                    /* The rocks of Bop are radioactive! */
                    vanAllen = Math.Exp(-mVessel.altitude / 200e3) * 0.2;
                    solar = 0.934;
                    galactic = 0.000346;
                    break;
                case 12: // Tylo
                    /* Another unmagnetic rock. */
                    vanAllen = 0.0;
                    solar = 0.893;
                    galactic = 0.0;
                    break;
                case 13: // Gilly
                    /* Some protection from cosmic rays, thanks to Eve's strong magnetosphere. */
                    vanAllen = 0.0;
                    solar = 1.0;
                    galactic = 0.336;
                    break;
                case 14: // Pol
                    /* No magnetic field. */
                    vanAllen = 0.0;
                    solar = 0.951;
                    galactic = 0.00352;
                    break;
                case 15: // Dres
                    /* Slight magnetic field, not much though */
                    vanAllen = 0.0;
                    solar = 0.8 + directSolarModel(10e3) * 0.2;
                    galactic = 0.4 + galacticModel(150e3) * 0.6;
                    break;
                case 16: // Eeloo
                    /* Currents of dissolved ions in Eeloo's icy mantle produce a magnetic field. */
                    vanAllen = vanAllenModel(250e3) * 0.02;
                    solar = directSolarModel(250e3);
                    galactic = galacticModel(12e6);
                    break;
                default:
                    vanAllen = 0.0;
                    solar = 1.0;
                    galactic = 1.0;
                    break;
            }
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
            strength *= TimeWarp.CurrentRate;
            int count = 1;
            if (strength >= 0.1)
                count = (int)Math.Ceiling(Core.Instance.mRandom.NextDouble() * 10.0 * strength);
            else if (Core.Instance.mRandom.NextDouble() > strength * 10.0)
                return; // count=0
            while (count > 1000) // at really high timewarps we can get huge counts.  Try to keep up
            {
                count -= 100;
                IrradiateOnce(100, source);
            }
            while (count > 100)
            {
                count -= 10;
                IrradiateOnce(10, source);
            }
            while (count-- > 0)
            {
                IrradiateOnce(1, source);
            }
        }

        private void IrradiateOnce(int count, RadiationSource source)
        {
            Vector3 aimPt = mVessel.CurrentCoM + randomVector(10.0f);
            Vector3 aimDir = randomVector(1e4f);
            double energy = 0;
            switch (source)
            {
                case RadiationSource.VanAllen: // low-energy
                    energy = 10.0 + Core.Instance.mRandom.NextDouble() * 150.0;
                    break;
                case RadiationSource.Solar: // medium-energy
                    energy = 120.0 + Core.Instance.mRandom.NextDouble() * 300.0;
                    aimDir = Sun.Instance.sunDirection;
                    aimDir.Normalize();
                    aimDir.Scale(new Vector3(1e4f, 1e4f, 1e4f));
                    break;
                case RadiationSource.Galactic: // high-energy
                    energy = (1.0 - 4.0 * Math.Log(Core.Instance.mRandom.NextDouble())) * 300.0;
                    break;
            }

            #if VERYDEBUG
            Logging.Log(String.Format("Casting ray at {0} from {1}, e={2:F3}", aimPt, -aimDir, energy), false);
            #endif

            List<RaycastHit> hits = new List<RaycastHit>(Physics.RaycastAll(aimPt - aimDir, aimDir, 2e4f));
            hits.Sort(RaycastSorter);

            foreach(RaycastHit rh in hits)
            {
                Part p = rh.transform.gameObject.GetComponent<Part>();
                if (p != null)
                {
                    int oldCount = count;
                    bool hasModule = false;
                    foreach(IKappaRayHandler h in p.FindModulesImplementing<IKappaRayHandler>())
                    {
                        hasModule = true;
                        if (count == 0) break;
                        count = h.OnRadiation(energy, count);
                    }
                    if (count > 0 && !hasModule)
                    {
                        double totalMass = p.mass + p.GetResourceMass();
                        double absorpCoeff = (1.0 - Math.Exp(-totalMass / 2.0)) / 2.0;
                        int absorbs = Modules.ModuleKappaRayAbsorber.absorbCount(count, absorpCoeff);
                        #if QUITEDEBUG
                        Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}, {3:D} absorbed", p.partInfo.title, count, energy, absorbs), false);
                        #endif
                        count -= absorbs;
                    }

                    if (count < oldCount)
                        p.AddThermalFlux((oldCount - count) * energy / 1e3);
                }
            }
        }
    }
}

