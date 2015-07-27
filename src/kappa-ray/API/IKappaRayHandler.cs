using System;

namespace kapparay
{
    public interface IKappaRayHandler
    {
        int OnRadiation(double energy, int count);
    }
}