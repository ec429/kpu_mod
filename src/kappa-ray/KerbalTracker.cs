using System;

namespace kapparay
{
    public class KerbalTracker
    {
        public Kerbal kerbal { get {
            KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;
            foreach (ProtoCrewMember c in roster.Kerbals(ProtoCrewMember.KerbalType.Crew))
            {
                Kerbal k = c.KerbalRef;
                if (k.crewMemberName.Equals(name))
                    return k;
            }
            foreach (ProtoCrewMember c in roster.Kerbals(ProtoCrewMember.KerbalType.Tourist))
            {
                Kerbal k = c.KerbalRef;
                if (k.crewMemberName.Equals(name))
                    return k;
            }
            return null;
        }}
        private string name;
        private double mCancerTime = Double.PositiveInfinity;
        public bool hasCancer { get { return !Double.IsPositiveInfinity(mCancerTime); } }
        public double lifetimeDose = 0;

        public KerbalTracker(string n)
        {
            name = n;
        }

        public bool Update()
        {
            if (Planetarium.GetUniversalTime() > mCancerTime)
            {
                Logging.Message(String.Format("Bad news!  {0} has died of cancer.", kerbal.crewMemberName));
                kerbal.protoCrewMember.Die();
                return true;
            }
            return false;
        }

        public void OnSave(ConfigNode node)
        {
            node.AddValue("name", name);
            if (hasCancer)
                node.AddValue("cancerTime", mCancerTime);
            node.AddValue("lifetimeDose", lifetimeDose);
        }

        public void OnLoad(ConfigNode node)
        {
            if (node.HasValue("cancerTime"))
                Double.TryParse(node.GetValue("cancerTime"), out mCancerTime);
            if (node.HasValue("lifetimeDose"))
                Double.TryParse(node.GetValue("lifetimeDose"), out lifetimeDose);
        }

        public int OnRadiation(double energy, int count)
        {
            if (count == 0) return count;
            Logging.Log(String.Format("{0} struck by {1:D} rays of energy {2:G}", kerbal.crewMemberName, count, energy), false);
            double pDeadly = (energy - 1e3) / 1e6;
            double pCancer = (1.0 - Math.Pow(energy / 400 - 1.25, 2.0)) * 1e-4;
            if (pDeadly > 0)
            {
                lifetimeDose += count * pDeadly;
                if (Core.Instance.mRandom.NextDouble() > Math.Pow(1.0 - pDeadly, count))
                {
                    Logging.Message(String.Format("Terrible news!  {0} has died of radiation sickness!", kerbal.crewMemberName));
                    kerbal.protoCrewMember.Die();
                    Core.Instance.ForgetKerbal(kerbal);
                    return 0;
                }
            }
            if (pCancer > 0)
            {
                lifetimeDose += count * pCancer;
                if (Core.Instance.mRandom.NextDouble() > Math.Pow(1.0 - pCancer, count))
                {
                    double nCancerTime = Planetarium.GetUniversalTime() + 6 * 3600 * (100.0 + Core.Instance.mRandom.NextDouble() * 1000.0);
                    Logging.Log(String.Format("{0} contracted cancer, life expectancy {1}", kerbal.crewMemberName, KSPUtil.PrintDate((int)nCancerTime, false)));
                    mCancerTime = Math.Min(mCancerTime, nCancerTime);
                }
            }
            return 0;
        }
    }
}

