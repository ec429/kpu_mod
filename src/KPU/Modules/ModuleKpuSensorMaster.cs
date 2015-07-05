using System;
using System.Text;

namespace KPU.Modules
{
    [KSPModule("KPU Sensor")]
    public class ModuleKpuSensorMaster : PartModule
    {
        // Checked for by InputValues in KPU.Processor

        [KSPField()]
        public double electricRate = 0;
        [KSPField()]
        public double maxAltitude = 0;
        [KSPField()]
        public string requireBody = "";
        [KSPField()]
        public bool isActive = true;

        public bool isWorking;
        public string GUI_status;

        private void setActive()
        {
            Events["EventToggle"].guiName = isActive ? "Deactivate" : "Activate";
            Events["EventToggle"].guiActive = true;
        }

        [KSPEvent(name = "EventToggle", guiName = "Toggle", guiActive = false)]
        public void EventToggle()
        {
            isActive = !isActive;
            setActive();
        }

        public override void OnStart(StartState state)
        {
            setActive();
        }

        public void FixedUpdate()
        {
            if (vessel == null)
                return;

            if (!isActive)
            {
                isWorking = false;
                GUI_status = "Inactive";
                return;
            }
            double resourceRequest = electricRate * TimeWarp.fixedDeltaTime;
            double electricUsage = part.RequestResource("ElectricCharge", resourceRequest);
            bool hasPower = electricUsage >= resourceRequest * 0.9;
            if (!hasPower)
            {
                GUI_status = "No power!";
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

            if (maxAltitude > 0)
                info.AppendFormat("Max. Altitude: {0}", Util.formatSI(maxAltitude, "m")).AppendLine();
            if (requireBody.Length > 0)
                info.AppendFormat("In orbit around: {0}", requireBody).AppendLine();
            if (electricRate > 0)
                info.AppendFormat("Energy req.: {0:F} charge/s", electricRate).AppendLine();

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

