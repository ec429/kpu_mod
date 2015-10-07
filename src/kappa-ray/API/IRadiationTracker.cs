using System;
using System.Collections.Generic;
using UnityEngine;

namespace kapparay
{
    public interface IRadiationTracker
    {
        // Utility functions
        Vector3 randomVector(); // Generate a random vector of length 1
        Vector3 randomVector(float length); // Generate a random vector of the given length
        // Functions to emit radiation
        // 'count' is the number of k-rays to fire, 'energy' is the energy each k-ray should have.
        // For comparison, Van Allen k-rays are about energy 100, direct solar around 300, and galactic cosmic rays are often over 1000
        void IrradiateVector(int count, double energy, Vector3 from, Vector3 dir); // Fire k-rays from 'from' in direction 'dir'
        void IrradiateFromPart(int count, double energy, Part p); // Fire k-rays from the given part in a random direction
    }
}

