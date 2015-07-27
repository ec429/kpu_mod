using System;

namespace kapparay.Modules
{
    [KSPModule("KappaRay Shielding")]
    public class ModuleKappaRayAbsorber : PartModule, IKappaRayHandler
    {
        [KSPField()]
        public double absorpCoeff;

        public static int absorbCount(int count, double coeff)
        {
            int absorbs;
            if (count * coeff > 1.0)
                absorbs = 1 + Core.Instance.mRandom.Next(1+(int)Math.Round(count * coeff)) + Core.Instance.mRandom.Next((int)Math.Round(count * coeff));
            else
                absorbs = Core.Instance.mRandom.NextDouble() < count * coeff ? 1 : 0;
            absorbs = Util.Clamp(absorbs, 0, count);
            return absorbs;
        }

        public int OnRadiation(double energy, int count)
        {
            int absorbs = absorbCount(count, absorpCoeff);
            Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}, {3:D} absorbed", part.partInfo.title, count, energy, absorbs), false);
            return count - absorbs;
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("absorpCoeff"))
                Double.TryParse(node.GetValue("absorpCoeff"), out absorpCoeff);
        }

        public override void OnSave(ConfigNode node)
        {
            node.AddValue("absorpCoeff", absorpCoeff);
        }
    }
}

