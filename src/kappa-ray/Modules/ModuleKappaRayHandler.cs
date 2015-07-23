using System;

namespace kapparay.Modules
{
    [KSPModule("KappaRay Affected")]
    public class ModuleKappaRayHandler : PartModule, IDisposable
    {
        public RadiationTracker rt;

        public override void OnStart(StartState state)
        {
            rt = Core.Instance.getRT(vessel);
        }

        public virtual int OnRadiation(double energy, int count)
        {
            Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}", part.partName, count, energy));
            return count; // all of them passed through us, none were absorbed
        }

        public void Dispose()
        {
            // TODO tell the RT we're not running any more
        }
    }
}

