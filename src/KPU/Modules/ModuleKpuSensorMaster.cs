using System;
using System.Text;

namespace KPU.Modules
{
    [KSPModule("KPU Sensor")]
    public class ModuleKpuSensorMaster : PartModule, kapparay.IKappaRayHandler
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
        public bool isActive = true;

        public double errorBar;

        // For kapparay.IKappaRayHandler
        public int OnRadiation(double energy, int count)
        {
            if (kapparay.Core.Instance.mRandom.NextDouble() < Math.Log10(energy) / 4.0)
            {
                errorBar += count * 12.0;
                return 0;
            }
            return count;
        }

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

        public void Update()
        {
            errorBar *= 0.995;
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
            if (sunDegrees > 0)
            {
                if (!vessel.orbit.referenceBody.name.Equals("Sun") && vessel.directSunlight)
                {
                    // TODO figure out how to compute Sun angle...
                    /*Vector3d toParent = vessel.orbit.pos;
                    Vector3d parentToSun = Vector3d.zero;
                    CelestialBody currBody = vessel.orbit.referenceBody;
                    while (!currBody.orbit.referenceBody.name.Equals("Sun"))
                    {
                        // something something co-ordinate transforms
                        parentToSun += currBody.orbit.pos;
                        currBody = currBody.orbit.referenceBody;
                    }
                    // Something something dot/cross product something atan
                    if (sunAngle < sunDegrees)
                    {
                        GUI_status = "Blinded by sunlight";
                        isWorking = false;
                        return;
                    }
                    */
                }
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
            if (sunDegrees > 0)
                info.AppendFormat("Min. angle to Sun: {0}°", sunDegrees).AppendLine();
            if (electricRate > 0)
                info.AppendFormat("Energy req.: {0:F} charge/s", electricRate).AppendLine();

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}

