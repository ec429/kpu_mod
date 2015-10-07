using System;
using System.Collections.Generic;
using UnityEngine;

namespace kapparay.Modules
{
    [KSPModule("KappaRay Emitter")]
    public class ModuleKappaRayEmitter : PartModule
    {
        [KSPField()]
        public double throttleCoeff = 0;

        private double lastUpdate = -1;

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("throttleCoeff"))
                Double.TryParse(node.GetValue("throttleCoeff"), out throttleCoeff);
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue("throttleCoeff", throttleCoeff);
        }

        public void Irradiate(double strength)
        {
            int count = 1;
            if (strength >= 0.1)
                count = (int)Math.Ceiling(Core.Instance.mRandom.NextDouble() * 10.0 * strength);
            else if (Core.Instance.mRandom.NextDouble() > strength * 10.0)
                return; // count=0
            while (count > 1000) // at really high timewarps we can get huge counts.  Try to keep up
            {
                count -= 100;
                IrradiateOnce(100);
            }
            while (count > 100)
            {
                count -= 10;
                IrradiateOnce(10);
            }
            while (count-- > 0)
            {
                IrradiateOnce(1);
            }
        }

        public void IrradiateOnce(int count)
        {
            RadiationTracker rt = Core.Instance.getRT(vessel);
            double energy = 180.0 + Core.Instance.mRandom.NextDouble() * 400.0; // slightly higher energy than solar
            rt.IrradiateFromPart(count, energy, part);
        }

        public override void OnFixedUpdate()
        {
            double t = Planetarium.GetUniversalTime();
            if (lastUpdate >= 0)
            {
                if (throttleCoeff > 0)
                {
                    ModuleEngines e = part.FindModuleImplementing<ModuleEngines>();
                    if (e.isOperational)
                    {
                        double strength = throttleCoeff * e.requestedThrottle * 100.0;
                        Irradiate(strength);
                    }
                }
            }
            lastUpdate = t;
        }

        public override string GetInfo()
        {
            return String.Format("Emits {0:G} per throttle %", throttleCoeff * 100.0);
        }
    }
}

