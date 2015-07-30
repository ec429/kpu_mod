using System;

namespace kapparay
{
    [KSPModule("KappaRay Solar Panel")]
    public class ModuleKappaRaySolarPanel : ModuleDeployableSolarPanel, IKappaRayHandler
    {
        [KSPField()]
        public float initChargeRate;

        public int OnRadiation(double energy, int count)
        {
            Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}", part.partInfo.title, count, energy), false);
            if (Core.Instance.mRandom.NextDouble() < Math.Exp(-50.0/energy))
            {
                if (initChargeRate < chargeRate)
                    initChargeRate = chargeRate;
                chargeRate *= (float)Math.Pow(1.0 - 1.0 / (initChargeRate * 2e5f), count);
                Logging.Log(String.Format("{0} degraded chargeRate to {1:G}", part.partInfo.title, chargeRate), false);
                return 0;
            }
            return count;
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("chargeRate"))
                Single.TryParse(node.GetValue("chargeRate"), out chargeRate);
            if (node.HasValue("initChargeRate"))
                Single.TryParse(node.GetValue("initChargeRate"), out initChargeRate);
            else
                initChargeRate = chargeRate;
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("initChargeRate", initChargeRate);
            node.AddValue("chargeRate", chargeRate);
        }
    }
}

