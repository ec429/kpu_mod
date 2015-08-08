using System;

namespace kapparay.Modules
{
    [KSPModule("KappaRay EVA")]
    public class ModuleKappaRayEVA : PartModule, IKappaRayHandler
    {
        public int OnRadiation(double energy, int count, System.Random random)
        {
            #if QUITEDEBUG
            Logging.Log(String.Format("EVA {0} struck by {1:D} rays of energy {2:G}", part.partInfo.title, count, energy), false);
            #endif
            foreach (ProtoCrewMember cm in vessel.GetVesselCrew())
            {
                KerbalTracker kt = Core.Instance.getKT(cm);
                count = kt.OnRadiation(energy, count);
            }

            return count;
        }
    }
}