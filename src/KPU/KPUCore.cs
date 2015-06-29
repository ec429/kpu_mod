using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace KPU
{
    public abstract class KPUCore : MonoBehaviour
    {
        public static KPUCore Instance { get; protected set; }

        /// Addons
        public AddOns.ControlLockAddon ctrlLockAddon { get; protected set; }

        public event Action OnGuiUpdate = delegate { };

        public void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            ctrlLockAddon = new AddOns.ControlLockAddon();

            Logging.Log("KPUCore loaded successfully.");
        }

        public void OnGUI()
        {
            GUI.depth = 0;
            OnGuiUpdate.Invoke();

            Action windows = delegate { };
            foreach (var window in UI.AbstractWindow.Windows.Values)
            {
                windows += window.Draw;
            }
            windows.Invoke();
        }

        public void OnDestroy()
        {
            Instance = null;
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KPUCoreFlight : KPUCore
    {
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class KPUCoreTracking : KPUCore
    {
    }
}
