using System;

namespace kapparay
{
    public class KerbalTracker
    {
        public Kerbal kerbal;
        private double mCancerTime = Double.PositiveInfinity;
        public bool hasCancer { get { return !Double.IsPositiveInfinity(mCancerTime); } }
        public double lifetimeDose = 0;

        // TODO loading and saving mCancerTime, lifetimeDose

        public KerbalTracker(Kerbal k)
        {
            kerbal = k;
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

        public int OnRadiation(double energy, int count)
        {
            if (count == 0) return count;
            double pDeadly = (energy - 1e3) / 1e6;
            double pCancer = (1.0 - Math.Pow(energy / 400 - 1.25, 2.0)) * 1e-5;
            if (pDeadly > 0)
            {
                if (Core.Instance.mRandom.NextDouble() > Math.Pow(1.0 - pDeadly, count))
                {
                    Logging.Message(String.Format("Terrible news!  {0} has died of radiation sickness!", kerbal.crewMemberName));
                    kerbal.protoCrewMember.Die();
                    Core.Instance.ForgetKerbal(kerbal);
                    return count - 1;
                }
            }
            if (pCancer > 0)
            {
                if (Core.Instance.mRandom.NextDouble() > Math.Pow(1.0 - pCancer, count))
                {
                    double nCancerTime = Planetarium.GetUniversalTime() + 6 * 3600 * (100.0 + Core.Instance.mRandom.NextDouble() * 1000.0);
                    Logging.Log(String.Format("{0} contracted cancer, time {1}", kerbal.crewMemberName, nCancerTime));
                    mCancerTime = Math.Min(mCancerTime, nCancerTime);
                    return count - 1;
                }
            }
            return count;
        }
    }
}

