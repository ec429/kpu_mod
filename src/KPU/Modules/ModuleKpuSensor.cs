using System;
using System.Text;

namespace KPU.Modules
{
    [KSPModule("KPU Sensor Reading")]
    public class ModuleKpuSensor : PartModule, kapparay.IKappaRayHandler
    {
        // Checked for by InputValues in KPU.Processor

        [KSPField()]
        public string sensorType;
        [KSPField()]
        public double sensorRes;
        [KSPField()]
        public string sensorUnit = "";
        [KSPField()]
        public double maxAltitude = 0;
        [KSPField()]
        public string requireBody = "";
        [KSPField()]
        public double sunDegrees = 0;

        [KSPField(guiName = "Status", guiActive = true)]
        public string GUI_status = "Inactive";

        private ModuleKpuSensorMaster master { get {
            return part.FindModuleImplementing<ModuleKpuSensorMaster>();
        }}

        public bool isWorking;

        public double errorBar;

        // For kapparay.IKappaRayHandler
        public int OnRadiation(double energy, int count)
        {
            if (kapparay.Core.Instance.mRandom.NextDouble() < Math.Log10(energy) / 4.0)
            {
                errorBar += count;
                return 0;
            }
            return count;
        }

        public void FixedUpdate()
        {
            if (vessel == null)
                return;

            Fields["GUI_status"].guiName = sensorType;
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

            info.Append("Sensor: ");
            info.AppendLine(sensorType);
            if (sensorRes > 0)
                info.AppendFormat("Resolution: {0:G}{1}", sensorRes, sensorUnit).AppendLine();
            if (maxAltitude > 0)
                info.AppendFormat("Max. Altitude: {0}", Util.formatSI(maxAltitude, "m")).AppendLine();
            if (requireBody.Length > 0)
                info.AppendFormat("In orbit around: {0}", requireBody).AppendLine();
            if (sunDegrees > 0)
                info.AppendFormat("Min. angle to Sun: {0}°", sunDegrees).AppendLine();

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

