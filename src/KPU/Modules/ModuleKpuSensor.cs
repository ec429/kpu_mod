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

        public bool hasPower;

        public override void OnFixedUpdate()
        {
            if (vessel == null)
                return;

            // only handle onFixedUpdate if the ship is unpacked
            if (vessel.packed)
                return;

            double resourceRequest = electricRate * TimeWarp.fixedDeltaTime;
            double electricUsage = part.RequestResource("ElectricCharge", resourceRequest, ResourceFlowMode.ALL_VESSEL);
            hasPower = electricUsage >= resourceRequest * 0.9;
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.Append("Sensor: ");
            info.AppendLine(sensorType);
            if (sensorRes > 0)
                info.AppendFormat("Resolution: {0:G}{1}", sensorRes, sensorUnit).AppendLine();
            info.AppendFormat("Energy usage: {0:G} charge/s", electricRate).AppendLine();

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

