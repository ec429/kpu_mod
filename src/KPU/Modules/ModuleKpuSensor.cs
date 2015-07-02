using System;

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
        public float electricRate;
        [KSPField(guiName = "Energy usage")]
        public float electricUsage;

        public bool hasPower;

        public override void OnFixedUpdate()
        {
            if (vessel == null)
                return;

            // only handle onFixedUpdate if the ship is unpacked
            if (vessel.packed)
                return;

            float resourceRequest = electricRate * TimeWarp.fixedDeltaTime;
            electricUsage = part.RequestResource("ElectricCharge", resourceRequest);
            hasPower = electricUsage >= resourceRequest * 0.9;
        }
    }
}

