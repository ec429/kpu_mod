using System;

namespace kapparay.Modules
{
    [KSPModule("KappaRay Crew Pod")]
    public class ModuleKappaRayCrewPod : PartModule, IKappaRayHandler
    {
        public int OnRadiation(double energy, int count)
        {
            Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}", part.partInfo.title, count, energy), false);
            if (part.CrewCapacity > 0)
            {
                int target = Core.Instance.mRandom.Next(part.CrewCapacity);
                foreach(ProtoCrewMember cm in part.protoModuleCrew)
                {
                    if (cm.seatIdx == target)
                    {
                        KerbalTracker kt = Core.Instance.getKT(cm);
                        count = kt.OnRadiation(energy, count);
                    }
                }
            }
            return count;
        }
    }
}

