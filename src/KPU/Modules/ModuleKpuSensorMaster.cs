﻿using System;
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
        public double sunDegrees = 0;
        [KSPField()]
        public bool requireIP;
        [KSPField()]
        public bool requireRadio;
        [KSPField()]
        public bool isActive = true;

        [KSPField]
        public String TechRequired = "None";
        public bool Unlocked
        {
            get
            {
                return ResearchAndDevelopment.GetTechnologyState(TechRequired) == RDTech.State.Available || TechRequired.Equals("None");
            }
        }

        public bool isWorking;
        public string GUI_status;

        private void setActive()
        {
            Events["EventToggle"].guiName = isActive ? "Deactivate" : "Activate";
            Events["EventToggle"].guiActive = Unlocked;
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

            setActive();
            if (!Unlocked)
            {
                isWorking = false;
                GUI_status = "Tech needed";
                return;
            }
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
            if (requireRadio)
            {
                if (!RemoteTech.API.API.HasConnectionToKSC(vessel.id))
                {
                    GUI_status = "No signal source";
                    isWorking = false;
                    return;
                }
            }
            isWorking = true;
            GUI_status = "OK";
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            if (!Unlocked)
                info.AppendFormat("Requires tech: {0}", TechRequired).AppendLine();
            if (maxAltitude > 0)
                info.AppendFormat("Max. Altitude: {0}", Util.formatSI(maxAltitude, "m")).AppendLine();
            if (requireBody.Length > 0)
                info.AppendFormat("In orbit around: {0}", requireBody).AppendLine();
            if (sunDegrees > 0)
                info.AppendFormat("Min. angle to Sun: {0}", Util.formatAngle(sunDegrees)).AppendLine();
            if (requireIP)
                info.AppendLine("Requires Inertial Platform");
            if (electricRate > 0)
                info.AppendFormat("Energy req.: {0:F} charge/s", electricRate).AppendLine();

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

