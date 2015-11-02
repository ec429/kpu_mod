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

        public KPU.Library.Library library { get; protected set; }
        public KPU.UI.LibraryWindow libraryWindow { get; protected set; }

        public void openLibraryWindow(KPU.UI.CodeWindow cw)
        {
            if (libraryWindow != null)
                libraryWindow.Hide();
            libraryWindow = new KPU.UI.LibraryWindow(cw);
            libraryWindow.Show();
        }

        public void Start()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            library = new KPU.Library.Library();
            libraryWindow = null;

            ctrlLockAddon = new AddOns.ControlLockAddon();

            Logging.Log("KPUCore loaded successfully.");
        }

        public void Load(ConfigNode node)
        {
            if (node.HasNode("library"))
            {
                library.Load(node.GetNode("library"));
            }
        }

        public void Save(ConfigNode node)
        {
            ConfigNode ln = node.AddNode("library");
            library.Save(ln);
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

    [KSPScenario(ScenarioCreationOptions.AddToNewGames, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    public class ScenarioKPU : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            KPUCore.Instance.Save(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            KPUCore.Instance.Load(node);
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
