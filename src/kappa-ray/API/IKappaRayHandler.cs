using System;

namespace kapparay
{
    public interface IKappaRayHandler
    {
        // Function called when a k-ray hits the part.
        // 'count' is the number of k-rays, 'energy' is the energy of each k-ray.
        // For comparison, Van Allen k-rays are about energy 100, direct solar around 300, and galactic cosmic rays are often over 1000
        int OnRadiation(double energy, int count, System.Random random);
    }
}