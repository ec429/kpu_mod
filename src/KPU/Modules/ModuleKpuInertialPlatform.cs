using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace KPU.Modules
{
    [KSPModule("KPU Inertial Platform")]
    public class ModuleKpuInertialPlatform : PartModule
    {
        [KSPField(guiName = "Inertial Platform", guiActive = true)]
        public string GUI_status = "Inactive";

        private ModuleKpuSensorMaster master { get {
            return part.FindModuleImplementing<ModuleKpuSensorMaster>();
        }}

        public bool isWorking;

        [KSPField()]
        public double drift = 0;

        private double resForSensor(string type)
        {
            List<ModuleKpuSensor> sens = vessel.FindPartModulesImplementing<ModuleKpuSensor>().FindAll(m => m.isWorking && !m.fromIP && m.sensorType.Equals(type));
            if (sens.Count == 0)
                return Double.PositiveInfinity;
            return sens.Min(m => m.sensorRes);
        }

        private bool havePosition { get {
            // Mission Control knows where we are
            if (RemoteTech.API.API.HasConnectionToKSC(vessel.id))
                return true;
            // Do we know where we are?
            if (resForSensor("longitude") < 1.0 && resForSensor("latitude") < 1.0 && resForSensor("altitude") < 200.0)
                return true;
            // Sorry, I guess we'll just keep drifting...
            return false;
        }}

        private bool haveOrientation { get {
            // need 1 degree or better resolution.
            // exclude fromIP so we don't use ourself as a calibration source!
            return vessel.FindPartModulesImplementing<ModuleKpuOrientation>().Exists(m => m.isWorking && m.customHPR && !m.fromIP && m.resolution <= 1.0);
        }}

        public void FixedUpdate()
        {
            float tw = TimeWarp.fixedDeltaTime; // higher timewarp will slightly affect the results, but not much
            // (the 'exact' approach would involve e, but I don't think it's necessary)
            drift = (drift + 0.2 * tw) * (1 + 0.005 * tw); // reaches 100 after about 4.2 minutes

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

            if (havePosition && haveOrientation)
            {
                drift = 0;
                GUI_status = "Fix";
            }
            else if (drift > 100)
            {
                GUI_status = "High drift!";
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("drift", drift);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            string sDrift = node.GetValue("drift");
            if (sDrift != null)
                Double.TryParse(sDrift, out drift);
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("Inertial platform");

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

