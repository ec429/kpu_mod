using System;
using System.Text;

namespace KPU.Modules
{
    [KSPModule("KPU Inertial Platform")]
    public class ModuleKpuInertialPlatform : PartModule
    {
        /* Currently, this does nothing except support KpuSensors that have 'requireIP = 1' */
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
            isWorking = true;
            GUI_status = "OK";
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("Inertial platform");

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

