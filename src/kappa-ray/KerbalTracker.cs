using System;

namespace kapparay
{
    public class KerbalTracker
    {
        public ProtoCrewMember cm { get {
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
            foreach (ProtoCrewMember c in roster.Kerbals(ProtoCrewMember.KerbalType.Crew))
            {
                if (c.name.Equals(name))
                    return c;
            }
            foreach (ProtoCrewMember c in roster.Kerbals(ProtoCrewMember.KerbalType.Tourist))
            {
                if (c.name.Equals(name))
                    return c;
            }
            return null;
        }}
        public string name;
        private double mCancerTime = Double.PositiveInfinity;
        public bool hasCancer { get { return !Double.IsPositiveInfinity(mCancerTime); } }
        public double softDose = 0, hardDose = 0;

        public KerbalTracker(string n)
        {
            name = n;
        }

        public bool Update()
        {
            if (Object.ReferenceEquals(cm, null)) // causes include death, firing or completed tourist itinerary
            {
                Logging.Log(String.Format("Forgetting {0}, no longer on roster", name));
                return true;
            }
            if (Planetarium.GetUniversalTime() > mCancerTime)
            {
                Logging.Message(String.Format("Bad news!  {0} has died of cancer.", name));
                cm.Die();
                return true;
            }
            return false;
        }

        public void OnSave(ConfigNode node)
        {
            node.AddValue("name", name);
            if (hasCancer)
                node.AddValue("cancerTime", mCancerTime);
            node.AddValue("softDose", softDose);
            node.AddValue("hardDose", hardDose);
        }

        public void OnLoad(ConfigNode node)
        {
            if (node.HasValue("cancerTime"))
                Double.TryParse(node.GetValue("cancerTime"), out mCancerTime);
            if (node.HasValue("lifetimeDose")) // For compatibility with older saves
                Double.TryParse(node.GetValue("lifetimeDose"), out softDose);
            if (node.HasValue("softDose"))
                Double.TryParse(node.GetValue("softDose"), out softDose);
            if (node.HasValue("hardDose"))
                Double.TryParse(node.GetValue("hardDose"), out softDose);
        }

        public int OnRadiation(double energy, int count)
        {
            if (count == 0) return count;
            Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}", name, count, energy), false);
            double pDeadly = (energy - 1e3) / 1e6;
            double pCancer = (1.0 - Math.Pow(energy / 400 - 1.25, 2.0)) * 1e-4;
            if (pDeadly > 0)
            {
                hardDose += count * pDeadly;
                if (Core.Instance.mRandom.NextDouble() > Math.Pow(1.0 - pDeadly, count))
                {
                    Logging.Message(String.Format("Terrible news!  {0} has died of radiation sickness!", name));
                    cm.Die();
                    Core.Instance.ForgetKerbal(this);
                    return 0;
                }
            }
            if (pCancer > 0)
            {
                softDose += count * pCancer;
                if (Core.Instance.mRandom.NextDouble() > Math.Pow(1.0 - pCancer, count))
                {
                    double nCancerTime = Planetarium.GetUniversalTime() + 6 * 3600 * (100.0 + Core.Instance.mRandom.NextDouble() * 1000.0);
                    Logging.Log(String.Format("{0} contracted cancer, life expectancy {1}", name, KSPUtil.PrintDate((int)nCancerTime, false)));
                    mCancerTime = Math.Min(mCancerTime, nCancerTime);
                }
            }
            return 0;
        }
    }
}

