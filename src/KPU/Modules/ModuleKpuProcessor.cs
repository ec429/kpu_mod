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

        [KSPField]
        public double electricRate;

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

        KPU.UI.CodeWindow mCodeWindow;
        KPU.UI.WatchWindow mWatchWindow;

        [KSPEvent(name = "EventEdit", guiName = "Edit Program", guiActive = true, guiActiveUnfocused = true)]
        public void EventEdit()
        {
            if (mProcessor == null)
            {
                Logging.Log("Tried to edit but no mProcessor");
            }
            else
            {
                if (mCodeWindow == null)
                    mCodeWindow = new KPU.UI.CodeWindow(mProcessor);
                mCodeWindow.Show();
            }
        }

        [KSPEvent(name = "EventUpload", guiName = "Upload Program", guiActive = true, guiActiveUnfocused = true)]
        public void EventUpload()
        {
            if (mProcessor == null)
            {
                Logging.Log("Tried to upload but no mProcessor");
            }
            else if (mCodeWindow == null)
            {
                Logging.Log("No editor active");
            }
            else if (mCodeWindow.mLoaded)
            {
                Logging.Log("Already loaded");
            }
            else if (!mCodeWindow.mCompiled)
            {
                Logging.Log("Code not compiled");
            }
            else if (mProcessor.isRunning)
            {
                Logging.Log("Halt the current program first");
            }
            else
            {
                mProcessor.ClearInstructions();
                bool ok = true;
                foreach (KPU.Processor.Instruction i in mCodeWindow.instructions)
                {
                    if (!mProcessor.AddInstruction(i.mText))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    Logging.Log("Program uploaded successfully");
                    mCodeWindow.mLoaded = true;
                }
                else
                {
                    mProcessor.ClearInstructions();
                    Logging.Log("IMEM has been cleared");
                }
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
            GameEvents.onVesselChange.Add(OnVesselChange);
            setRunning();
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("isRunning", isRunning);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (mProcessor == null)
                        mProcessor = new Processor.Processor(part, this);
                    mProcessor.Save(node);
                }
            }
            catch (Exception e) { Logging.Log(e.ToString()); }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            string sIsRunning = node.GetValue("isRunning");
            if (sIsRunning != null)
                bool.TryParse(sIsRunning, out isRunning);
            setRunning();

            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (mProcessor == null)
                        mProcessor = new Processor.Processor(part, this);
                    mProcessor.Load(node);
                }
            }
            catch (Exception e) { Logging.Log(e.ToString()); };
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

            if (mProcessor != null)
                mProcessor.OnFixedUpdate();

            // Re-attach periodically
            vessel.OnFlyByWire -= OnFlyByWirePost;
            vessel.OnFlyByWire = vessel.OnFlyByWire + OnFlyByWirePost;

            double resourceRequest = electricRate * TimeWarp.fixedDeltaTime;
            double electricUsage = part.RequestResource("ElectricCharge", resourceRequest, ResourceFlowMode.ALL_VESSEL);
            mProcessor.hasPower = electricUsage >= resourceRequest * 0.9;
        }

        public void OnVesselChange(Vessel v)
        {
            if (mWatchWindow != null)
            {
                mWatchWindow.Hide();
            }
            if (mCodeWindow != null)
            {
                mCodeWindow.Hide();
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
            info.AppendFormat("Energy usage: {0:G} charge/s", electricRate).AppendLine();

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

            if (mCodeWindow != null)
            {
                mCodeWindow.Hide();
            }
        }
    }
}

