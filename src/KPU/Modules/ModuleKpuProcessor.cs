using System;
using System.Text;
using UnityEngine;

namespace KPU.Modules
{
    [KSPModule("KPU Processor")]
    public class ModuleKpuProcessor : PartModule, IDisposable
    {
        public Processor.Processor mProcessor = null;

        [KSPField()]
        public bool hasLevelTrigger;
        [KSPField()]
        public bool hasLogicOps;
        [KSPField()]
        public bool hasArithOps;
        [KSPField()]
        public int imemWords;
        [KSPField()]
        public int latches;
        [KSPField()]
        public int timers;

        [KSPField()]
        public double electricRate;

        [KSPField()]
        public bool isRunning = false;
        [KSPField(guiName = "Status")]
        public string GUI_status;
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

        [KSPAction("Run Program")]
        public void ActionRun(KSPActionParam param)
        {
            isRunning = true;
            setRunning();
        }
        [KSPAction("Halt Program")]
        public void ActionHalt(KSPActionParam param)
        {
            isRunning = false;
            setRunning();
        }
        [KSPAction("Toggle Program")]
        public void ActionToggle(KSPActionParam param)
        {
            EventToggle();
        }

        KPU.UI.CodeWindow mCodeWindow;
        KPU.UI.WatchWindow mWatchWindow;

        [KSPEvent(name = "EventEdit", guiName = "Edit Program", guiActive = true, guiActiveUnfocused = true, guiActiveEditor = true)]
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

        [KSPEvent(name = "EventUpload", guiName = "Upload Program", guiActive = true, guiActiveUnfocused = true, guiActiveEditor = true)]
        public void EventUpload()
        {
            if (mProcessor == null)
            {
                Logging.Log("Tried to upload but no mProcessor");
            }
            else if (mCodeWindow == null)
            {
                Logging.Message("No editor active");
            }
            else if (mCodeWindow.mLoaded)
            {
                Logging.Message("Already loaded");
            }
            else if (!mCodeWindow.mCompiled)
            {
                Logging.Message("Code not compiled");
            }
            else if (mProcessor.isRunning)
            {
                Logging.Message("Halt the current program first");
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
                    Logging.Message("Program uploaded successfully");
                    mCodeWindow.mLoaded = true;
                }
                else
                {
                    mProcessor.ClearInstructions();
                    Logging.Message("IMEM has been cleared");
                }
                UpdateInfo();
            }
        }

        [KSPEvent(name = "EventOpen", guiName = "Watch Display", guiActive = true, guiActiveUnfocused = true)]
        public void EventOpen()
        {
            if (mProcessor == null)
            {
                Logging.Log("Tried to open but no mProcessor");
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
                if (mProcessor == null)
                    mProcessor = new Processor.Processor(part, this);
                mProcessor.Save(node);
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
                if (mProcessor == null)
                    mProcessor = new Processor.Processor(part, this);
                mProcessor.Load(node);
                UpdateInfo();
            }
            catch (Exception e) { Logging.Log(e.ToString()); };
        }

        public void UpdateInfo()
        {
            if (mProcessor != null)
            {
                GUI_imemWords = mProcessor.imemWords;
                Fields["GUI_imemWords"].guiActive = true;
                Fields["GUI_imemWords"].guiActiveEditor = true;
                GUI_status = mProcessor.isRunning ? mProcessor.isHibernating ? "Hibernating" : "Running" : "Inactive";
                Fields["GUI_status"].guiActive = true;
            }
            else
            {
                GUI_imemWords = -1;
                Fields["GUI_imemWords"].guiActive = false;
                Fields["GUI_imemWords"].guiActiveEditor = false;
                Fields["GUI_status"].guiActive = false;
            }
        }

        public override void OnUpdate()
        {
            if (mProcessor != null)
                mProcessor.OnUpdate();
            UpdateInfo();
        }

        public void OnFlyByWirePost(FlightCtrlState fcs)
        {
            if (mProcessor != null)
                mProcessor.OnFlyByWire(fcs);
        }

        public void FixedUpdate()
        {
            if (!(HighLogic.fetch && HighLogic.LoadedSceneIsFlight))
                return;

            if (vessel == null)
                return;

            if (mProcessor == null)
                return;

            double scale = mProcessor.isRunning ? mProcessor.isHibernating ? 0.1f : 1.0f : 0.02f;
            double resourceRequest = electricRate * TimeWarp.fixedDeltaTime * scale;
            double electricUsage = part.RequestResource("ElectricCharge", resourceRequest);
            part.AddThermalFlux(electricUsage * 40.0 / TimeWarp.fixedDeltaTime);
            mProcessor.hasPower = electricUsage >= resourceRequest * 0.9;

            if (vessel.packed)
                return;

            mProcessor.OnFixedUpdate();

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
            info.AppendFormat("IMEM: {0:D} words", imemWords).AppendLine();
            if (latches > 0)
                info.AppendFormat("Latches: {0:D}", latches).AppendLine();
            info.AppendFormat("Energy usage: {0:G} charge/s", electricRate).AppendLine();

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public void Dispose()
        {
            KPU.Logging.Log("ModuleKpuProcessor: Dispose");

            GameEvents.onVesselChange.Remove(OnVesselChange);

            if (mWatchWindow != null)
                mWatchWindow.Hide();

            if (mCodeWindow != null)
                mCodeWindow.Hide();

            if (mProcessor != null)
                mProcessor.isRunning = false;
        }
    }
}

