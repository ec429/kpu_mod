using System;

namespace kapparay
{
    public class RadiationTracker
    {
        public Vessel vessel;

        private Random mRandom;

        public RadiationTracker (Vessel v)
        {
            vessel = v;
            mRandom = new Random();
        }

        public void Update()
        {
            // For now, just bathe all vessels in ambient radiation, that can't even be shielded against by other parts
            foreach(Part p in vessel.Parts)
            {
                int count = mRandom.Next(10);
                double energy = 100.0 + mRandom.NextDouble() * 1500.0;
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

