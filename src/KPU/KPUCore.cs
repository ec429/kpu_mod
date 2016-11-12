using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace KPU
{
    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class KPUCore : MonoBehaviour
    {
        public static KPUCore Instance { get; protected set; }

        /// Addons
        public AddOns.ControlLockAddon ctrlLockAddon { get; protected set; }

        public event Action OnGuiUpdate = delegate { };

        public KPU.Library.Library library { get { return ScenarioKPU.Instance.library; } }
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

            libraryWindow = null;

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

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.EDITOR)]
    public class ScenarioKPU : ScenarioModule
    {
        public static ScenarioKPU Instance {get; protected set; }
        public KPU.Library.Library library { get; protected set; }

        public override void OnAwake()
        {
            Instance = this;
            library = new KPU.Library.Library();
            base.OnAwake();
        }

        public override void OnSave(ConfigNode node)
        {
            ConfigNode ln = node.AddNode("library");
            library.Save(ln);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (node.HasNode("library"))
            {
                library.Load(node.GetNode("library"));
            }
        }
    }
}
