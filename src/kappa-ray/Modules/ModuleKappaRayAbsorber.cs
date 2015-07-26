using System;

namespace kapparay.Modules
{
    [KSPModule("KappaRay Shielding")]
    public class ModuleKappaRayAbsorber : ModuleKappaRayHandler
    {
        [KSPField()]
        public double absorpCoeff;

        public override int OnRadiation(double energy, int count)
        {
            int absorbs;
            if (count * absorpCoeff > 1.0)
                absorbs = 1 + Core.Instance.mRandom.Next(1+(int)Math.Round(count * absorpCoeff)) + Core.Instance.mRandom.Next((int)Math.Round(count * absorpCoeff));
            else
                absorbs = Core.Instance.mRandom.NextDouble() < count * absorpCoeff ? 1 : 0;
            absorbs = Util.Clamp(absorbs, 0, count);
            part.AddThermalFlux(absorbs * energy / 1e3);
            Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}, {3:D} absorbed", part.partName, count, energy, absorbs), false);
            return count - absorbs;
        }
    }
}

