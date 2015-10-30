using System;
using System.Text;
using System.Linq;

namespace KPU.Modules
{
    [KSPModule("KPU Orientation Source")]
    public class ModuleKpuOrientation : PartModule
    {
        [KSPField()]
        public bool srfVertical;
        [KSPField()]
        public bool orbVertical;
        [KSPField()]
        public bool srfPrograde;
        [KSPField()]
        public bool orbPrograde;
        [KSPField()]
        public bool customHP;
        [KSPField()]
        public bool customHPR;
        [KSPField()]
        public double inherentRes = 1;
        [KSPField()]
        public double maxAltitude = 0;
        [KSPField()]
        public string requireBody = "";
        [KSPField()]
        public double sunDegrees = 0;
        [KSPField()]
        public bool requireIP;
        [KSPField()]
        public bool fromIP;

        [KSPField(guiName = "Status", guiActive = true)]
        public string GUI_status = "Inactive";

        private ModuleKpuSensorMaster master { get {
            return part.FindModuleImplementing<ModuleKpuSensorMaster>();
        }}

        private double lossFactor = 1.0;

        public double resolution
        {
            get
            {
                return inherentRes * lossFactor;
            }
            set
            {
                inherentRes = value;
            }
        }

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
            if (sunDegrees > 0)
            {
                if (!vessel.orbit.referenceBody.name.Equals("Sun") && vessel.directSunlight)
                {
                    Vector3d sun = -FlightGlobals.Bodies[0].position.xzy, toParent = vessel.orbit.pos;
                    double sunAngle = Vector3d.Angle(sun, toParent);
                    if (sunAngle < sunDegrees)
                    {
                        GUI_status = "Blinded by sunlight";
                        isWorking = false;
                        return;
                    }
                }
            }
            if (requireIP)
            {
                if (!vessel.FindPartModulesImplementing<ModuleKpuInertialPlatform>().Exists(m => m.isWorking))
                    isWorking = false;
            }
            if (fromIP)
            {
                double drift = part.FindModulesImplementing<ModuleKpuInertialPlatform>().ConvertAll<double>(m => m.drift).Min();
                lossFactor = 1.0 + drift / 25.0; // a drift of 100 degrades resolution by a factor of 5
            }
            isWorking = true;
            GUI_status = "OK";
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            if (srfVertical)
                info.AppendLine("Gives srfVertical");
            if (orbVertical)
                info.AppendLine("Gives orbVertical");
            if (srfPrograde)
                info.AppendLine("Gives srfPrograde, srfRetrograde");
            if (orbPrograde)
                info.AppendLine("Gives orbPrograde, orbRetrograde");
            if (customHP)
                info.AppendLine("Gives customHP");
            if (customHPR)
                info.AppendLine("Gives customHPR");
            if (resolution > 0)
                info.AppendFormat("Resolution: {0}", Util.formatAngle(resolution)).AppendLine();
            if (maxAltitude > 0)
                info.AppendFormat("Max. Altitude: {0}", Util.formatSI(maxAltitude, "m")).AppendLine();
            if (requireBody.Length > 0)
                info.AppendFormat("In orbit around: {0}", requireBody).AppendLine();
            if (sunDegrees > 0)
                info.AppendFormat("Min. angle to Sun: {0}", Util.formatAngle(sunDegrees)).AppendLine();
            if (requireIP)
                info.AppendLine("Requires Inertial Platform");
            if (fromIP)
                info.AppendLine("Subject to Inertial Platform drift");

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

