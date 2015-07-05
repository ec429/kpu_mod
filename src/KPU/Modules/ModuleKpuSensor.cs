using System;
using System.Text;

namespace KPU.Modules
{
    [KSPModule("KPU Sensor")]
    public class ModuleKpuSensor : PartModule
    {
        // Checked for by InputValues in KPU.Processor

        [KSPField()]
        public string sensorType;
        [KSPField()]
        public double sensorRes;
        [KSPField()]
        public string sensorUnit = "";
        [KSPField()]
        public double electricRate;
        [KSPField()]
        public double maxAltitude = 0;
        [KSPField()]
        public string requireBody = "";

        [KSPField(guiName = "Status", guiActive = true)]
        public string GUI_status = "Inactive";

        public bool hasPower;
        public bool isWorking;

        public override void OnFixedUpdate()
        {
            if (vessel == null)
                return;

            // only handle onFixedUpdate if the ship is unpacked
            if (vessel.packed)
                return;

            Fields["GUI_status"].guiName = sensorType;
            double resourceRequest = electricRate * TimeWarp.fixedDeltaTime;
            double electricUsage = part.RequestResource("ElectricCharge", resourceRequest, ResourceFlowMode.ALL_VESSEL);
            hasPower = electricUsage >= resourceRequest * 0.9;
            isWorking = false;
            if (hasPower)
            {
                if (requireBody.Equals("") || vessel.orbit.referenceBody.name.Equals(requireBody))
                {
                    if (maxAltitude == 0 || vessel.altitude < maxAltitude)
                    {
                        isWorking = true;
                        GUI_status = "OK";
                    }
                    else
                    {
                        GUI_status = "Too high!";
                    }
                }
                else
                {
                    GUI_status = string.Format("Not at {0}!", requireBody);
                }
            }
            else
            {
                GUI_status = "No power!";
            }
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
            if (requireBody != null)
                info.AppendFormat("In orbit around: {0}", requireBody).AppendLine();
            info.AppendFormat("Energy usage: {0:G} charge/s", electricRate).AppendLine();

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

