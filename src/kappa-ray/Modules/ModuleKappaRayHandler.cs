using System;

namespace kapparay.Modules
{
    [KSPModule("KappaRay Affected")]
    public class ModuleKappaRayHandler : PartModule
    {
        public RadiationTracker rt;

        public override void OnStart(StartState state)
        {
            rt = Core.Instance.getRT(vessel);
        }

        public virtual int OnRadiation(double energy, int count)
        {
            Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}", part.partInfo.title, count, energy), false);
            if (part.CrewCapacity > 0)
            {
                int target = Core.Instance.mRandom.Next(part.CrewCapacity);
                foreach(ProtoCrewMember cm in part.protoModuleCrew)
                {
                    if (cm.seatIdx == target)
                    {
                        KerbalTracker kt = Core.Instance.getKT(cm.KerbalRef);
                        count = kt.OnRadiation(energy, count);
                    }
                }
            }
            return count;
        }
    }
}

