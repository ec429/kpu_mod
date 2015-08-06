using System;
using System.Text;

namespace KPU.Modules
{
    [KSPModule("KPU Orientation Source")]
    public class ModuleKpuOrientation : PartModule
    {
        [KSPField()]
        public int srfVertical;
        [KSPField()]
        public int orbVertical;
        [KSPField()]
        public int srfPrograde;
        [KSPField()]
        public int orbPrograde;
        [KSPField()]
        public int customHP;
        [KSPField()]
        public double resolution = 1; // not currently used
        [KSPField()]
        public double maxAltitude = 0;
        [KSPField()]
        public string requireBody = "";

        [KSPField(guiName = "Status", guiActive = true)]
        public string GUI_status = "Inactive";

        private ModuleKpuSensorMaster master { get {
            return part.FindModuleImplementing<ModuleKpuSensorMaster>();
        }}

        public bool isWorking;

        public void FixedUpdate()
        {
            if (vessel == null)
                return;

            if (master != null && !master.isWorking)
            {
                GUI_status = master.GUI_status;
                isWorking = false;
                return;
            }
            if (requireBody.Length > 0 && !vessel.orbit.referenceBody.name.Equals(requireBody))
            {
                GUI_status = string.Format("Not at {0}!", requireBody);
                isWorking = false;
                return;
            }
            if (maxAltitude > 0 && vessel.altitude > maxAltitude)
            {
                GUI_status = "Too high!";
                isWorking = false;
                return;
            }
            isWorking = true;
            GUI_status = "OK";
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("Orientation source:");
            if (srfVertical > 0)
                info.AppendLine("Gives srfVertical");
            if (orbVertical > 0)
                info.AppendLine("Gives orbVertical");
            if (srfPrograde > 0)
                info.AppendLine("Gives srfPrograde, srfRetrograde");
            if (orbPrograde > 0)
                info.AppendLine("Gives orbPrograde, orbRetrograde");
            if (customHP > 0)
                info.AppendLine("Gives customHP");
            if (resolution > 0)
                info.AppendFormat("Resolution: {0:G}°", resolution).AppendLine();
            if (maxAltitude > 0)
                info.AppendFormat("Max. Altitude: {0}", Util.formatSI(maxAltitude, "m")).AppendLine();
            if (requireBody.Length > 0)
                info.AppendFormat("In orbit around: {0}", requireBody).AppendLine();

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

