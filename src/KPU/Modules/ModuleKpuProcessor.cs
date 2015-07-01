using System;
using System.Text;
using UnityEngine;

namespace KPU.Modules
{
    [KSPModule("KPU Processor")]
    public class ModuleKpuProcessor : PartModule, IDisposable
    {
        private Processor.Processor mProcessor = null;

        [KSPField()]
        public bool hasLevelTrigger;
        [KSPField()]
        public bool hasLogicOps;
        [KSPField()]
        public bool hasArithOps;
        [KSPField()]
        public int imemWords;

        [KSPField()]
        public bool isRunning = false;
        [KSPField(guiName = "IMEM free")]
        public int GUI_imemWords;

        private void setRunning()
        {
            if (mProcessor != null)
                mProcessor.isRunning = isRunning;
            Events["EventToggle"].guiName = isRunning ? "Halt Program" : "Run Program";
            Events["EventToggle"].guiActive = true;
        }

        [KSPEvent(name = "EventToggle", guiName = "Toggle Program", guiActive = false)]
        public void EventToggle()
        {
            isRunning = !isRunning;
            setRunning();
        }

        KPU.UI.WatchWindow mWatchWindow;

        [KSPEvent(name = "EventEdit", guiName = "Edit Program", guiActive = true, guiActiveUnfocused = true)]
        public void EventEdit()
        {
            if (mProcessor == null)
            {
                KPU.Logging.Log("Tried to edit but no mProcessor");
            }
            else
            {
                KPU.Logging.Log("EventEdit");
            }
        }

        [KSPEvent(name = "EventOpen", guiName = "Watch Display", guiActive = true, guiActiveUnfocused = true)]
        public void EventOpen()
        {
            if (mProcessor == null)
            {
                KPU.Logging.Log("Tried to open but no mProcessor");
            }
            else
            {
                if (mWatchWindow == null)
                    mWatchWindow = new KPU.UI.WatchWindow(mProcessor);
                mWatchWindow.Show();
            }
        }

        public override void OnStart(StartState state)
        {
            mProcessor = new Processor.Processor(part, this);
            GameEvents.onVesselChange.Add(OnVesselChange);
            setRunning();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (mProcessor == null)
                        mProcessor = new Processor.Processor(part, this);
                    mProcessor.Save(node);
                }

            }
            catch (Exception e) { print(e); }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (mProcessor == null)
                        mProcessor = new Processor.Processor(part, this);
                    mProcessor.Load(node);
                }
            }
            catch (Exception e) { print(e); };
        }

        public override void OnUpdate()
        {
            if (mProcessor != null)
            {
                mProcessor.OnUpdate();
                GUI_imemWords = mProcessor.imemWords;
                Fields["GUI_imemWords"].guiActive = true;
            }
            else
            {
                GUI_imemWords = -1;
                Fields["GUI_imemWords"].guiActive = false;
            }
        }

        public void OnFlyByWirePost(FlightCtrlState fcs)
        {
            if (mProcessor != null)
                mProcessor.OnFlyByWire(fcs);
        }

        public override void OnFixedUpdate()
        {
            if (vessel == null)
                return;

            // only handle onFixedUpdate if the ship is unpacked
            if (vessel.packed)
                return;

            // Re-attach periodically
            vessel.OnFlyByWire -= OnFlyByWirePost;
            vessel.OnFlyByWire = vessel.OnFlyByWire + OnFlyByWirePost;
        }

        public void OnVesselChange(Vessel v)
        {
            if (mWatchWindow != null)
            {
                mWatchWindow.Hide();
            }
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("Supports Edge-Triggers");
            if (hasLevelTrigger)
                info.AppendLine("Supports Level-Triggers");
            if (hasLogicOps)
                info.AppendLine("Supports Logical Ops");
            if (hasArithOps)
                info.AppendLine("Supports Arithmetic Ops");
            
            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public void Dispose()
        {
            KPU.Logging.Log("ModuleKpuProcessor: Dispose");

            GameEvents.onVesselChange.Remove(OnVesselChange);

            if (mWatchWindow != null)
            {
                mWatchWindow.Hide();
            }
        }
    }
}

