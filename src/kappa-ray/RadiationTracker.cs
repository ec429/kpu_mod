using System;
using System.Collections.Generic;
using UnityEngine;

namespace kapparay
{
    public class RadiationEnvironment
    {
        public enum ModelType { Corona, VanAllen, DirectSolar, Galactic, ScaledDS, ScaledG, DSPG, Surface, Fixed };
        public ModelType type;
        public List<double> parms;

        public RadiationEnvironment(ModelType t, List<double> p)
        {
            type = t;
            parms = p;
        }

        public static RadiationEnvironment Parse(string s)
        {
            ModelType t = ModelType.Fixed;
            List<double> p = new List<double>();
            foreach (string word in s.Split(new Char[] {' '}))
            {
                double v;
                if (word.Equals("Corona"))
                    t = ModelType.Corona;
                else if (word.Equals("VanAllen"))
                    t = ModelType.VanAllen;
                else if (word.Equals("DirectSolar"))
                    t = ModelType.DirectSolar;
                else if (word.Equals("Galactic"))
                    t = ModelType.Galactic;
                else if (word.Equals("ScaledDS"))
                    t = ModelType.ScaledDS;
                else if (word.Equals("ScaledG"))
                    t = ModelType.ScaledG;
                else if (word.Equals("DS+G"))
                    t = ModelType.DSPG;
                else if (word.Equals("Surface"))
                    t = ModelType.Surface;
                else if (word.Equals("Fixed"))
                    t = ModelType.Fixed;
                else if (Double.TryParse(word, out v))
                    p.Add(v);
                else
                    Logging.Log(String.Format("Failed to parse word {0} in RadiationEnvironment '{1}'", word, s));
            }
            return new RadiationEnvironment(t, p);
        }

        // Generic models for planetary radiation
        public double vanAllenModel(Vessel v, double altScale)
        {
            // TODO: support off-centre or tilted magnetic fields, like Earth has
            // (thereby producing something like the South Atlantic Anomaly)
            double magLat = v.latitude;
            altScale *= Math.Cos(magLat * Math.PI / 180.0 / 2.0);
            double magnetic = Math.Exp(-v.altitude / altScale);
            double magcap = (magnetic - magnetic * magnetic) * 4.0;
            double vaScale = 1.0 / (1.0 + Math.Pow(Math.Sin(magLat * Math.PI / 180.0), 2.0));
            return Math.Max(vaScale * magcap * magcap - v.atmDensity * 100.0, 0.0);
        }

        public double directSolarModel(Vessel v, double altScale)
        {
            double magnetic = Math.Exp(-v.altitude / altScale);
            return Math.Max(1.0 - magnetic * 1.12 - v.atmDensity * 100.0, 0.0);
        }

        public double galacticModel(Vessel v, double altScale)
        {
            if (v.altitude < 10) return 0;
            return Math.Max(Math.Exp(-altScale / v.altitude) - v.atmDensity, 0.0);
        }

        public double value(Vessel v)
        {
            switch(type)
            {
                case ModelType.Corona:
                    return parms[0] * Math.Exp(-v.altitude/parms[1]) * Core.Instance.mSolar.flux;
                case ModelType.VanAllen:
                    return vanAllenModel(v, parms[0]) * (parms.Count > 1 ? parms[1] : 1.0);
                case ModelType.DirectSolar:
                    return directSolarModel(v, parms[0]) * (parms.Count > 1 ? parms[1] : 1.0);
                case ModelType.Galactic:
                    return galacticModel(v, parms[0]);
                case ModelType.ScaledDS:
                    return ((directSolarModel(v, parms[0]) + parms[1]) / parms[2]);
                case ModelType.ScaledG:
                    return ((galacticModel(v, parms[0]) + parms[1]) / parms[2]);
                case ModelType.DSPG:
                    return directSolarModel(v, parms[0]) * parms[1] + galacticModel(v, parms[2]) * parms[3];
                case ModelType.Surface:
                    return Math.Exp(-v.altitude / parms[0]) * (parms.Count > 1 ? parms[1] : 1.0);
                case ModelType.Fixed:
                default:
                    return parms[0];
            }
        }
    }

    public class RadiationTracker : MonoBehaviour, IRadiationTracker
    {
        private Vessel mVessel;

        public double lastV, lastS, lastG;

        public override string ToString()
        {
            return "RadiationTracker(" + mVessel.vesselName + ")";
        }

        public RadiationTracker (Vessel v)
        {
            mVessel = v;
            lastV = lastS = lastG = Double.NaN;
        }

        public enum RadiationSource { VanAllen, Solar, Galactic };

        public void Update(double dT)
        {
            if (FlightDriver.Pause) return;
            if (!mVessel.isActiveVessel) return; /* Can't figure out how to handle background vessels reliably */
            double solarFlux = Core.Instance.mSolar.flux * mVessel.solarFlux / 1360.0; // scale solarFlux to kerbin==1
            bool directSolar = mVessel.directSunlight;
            CelestialBody planetID = mVessel.mainBody;
            double vanAllen, solar, galactic;
            string planetName = planetID.name;
            vanAllen = 0.0;
            solar = 1.0;
            galactic = 1.0;
            if (Core.Instance.va.ContainsKey(planetName))
                vanAllen = Core.Instance.va[planetName].value(mVessel);
            if (Core.Instance.so.ContainsKey(planetName))
                solar = Core.Instance.so[planetName].value(mVessel);
            if (Core.Instance.ga.ContainsKey(planetName))
                galactic = Core.Instance.ga[planetName].value(mVessel);
            Irradiate(vanAllen * dT * 50.0, RadiationSource.VanAllen);
            lastV = vanAllen;
            if (directSolar)
            {
                Irradiate(solar * solarFlux * dT * 50.0, RadiationSource.Solar);
                lastS = solar * solarFlux;
            }
            else
            {
                lastS = 0.0;
            }
            Irradiate(galactic * 0.05 * dT * 50.0, RadiationSource.Galactic);
            lastG = galactic * 0.05;
        }

        public Vector3 randomVector()
        {
            Vector3 v = new Vector3((float)(Core.Instance.mRandom.NextDouble() - 0.5),
                                    (float)(Core.Instance.mRandom.NextDouble() - 0.5),
                                    (float)(Core.Instance.mRandom.NextDouble() - 0.5));
            if (v.magnitude < 1e-6) // very unlikely
                v = Vector3.up;
            v.Normalize();
            return v;
        }

        public Vector3 randomVector(float length)
        {
            Vector3 v = randomVector();
            Vector3 mag = new Vector3(length, length, length);
            v.Scale(mag);
            return v;
        }

        private int RaycastSorter(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }

        public void Irradiate(double strength, RadiationSource source)
        {
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
            Vector3 aimDir = randomVector();
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

            IrradiateVector(count, energy, aimPt - aimDir * 1e4f, aimDir);
        }

        public void IrradiateFromPart(int count, double energy, Part p)
        {
            IrradiateVector(count, energy, p.partTransform.position, randomVector());
        }

        public void IrradiateVector(int count, double energy, Vector3 from, Vector3 dir)
        {
            List<RaycastHit> hits = new List<RaycastHit>(Physics.RaycastAll(from, dir, 2e4f));

            IrradiateList(count, energy, hits);
        }

        private void IrradiateList(int count, double energy, List<RaycastHit> hits)
        {
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
                        count = h.OnRadiation(energy, count, Core.Instance.mRandom);
                    }
                    if (count > 0)
                    {
                        double negAbsorpCoeff = hasModule ? 1.0 : Math.Exp(-Math.Pow(p.mass, 1/3.0) / 8.0); // implicit resAbsCe of 0.125 for structure, assuming density of 1
                        foreach (PartResource pr in p.Resources)
                        {
                            if (Core.Instance.resAbsCe.ContainsKey(pr.resourceName))
                            {
                                double nrac = Math.Exp(-Math.Pow(pr.amount, 1/3.0) * pr.info.density * Core.Instance.resAbsCe[pr.resourceName]);
                                negAbsorpCoeff *= nrac;
                            }
                        }
                        int absorbs = Modules.ModuleKappaRayAbsorber.absorbCount(count, 1.0 - negAbsorpCoeff);
                        #if QUITEDEBUG
                        Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}, {3:D} absorbed", p.partInfo.title, count, energy, absorbs), false);
                        #endif
                        count -= absorbs;
                    }

                    if (count < oldCount)
                        p.AddThermalFlux((oldCount - count) * energy / 1e3 / TimeWarp.fixedDeltaTime);
                }
            }
        }
    }
}

