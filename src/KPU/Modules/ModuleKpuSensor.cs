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
    }
}

