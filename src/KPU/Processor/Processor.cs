using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KPU.Processor
{
    public class Instruction
    {
        private string mText;

        public Instruction (string text)
        {
            mText = text;
            KPU.Logging.Log("Instruction: Parser not written yet!");
        }

        int imemWords { get { return 0; } }
    }

    public enum InputType { BOOLEAN, DOUBLE };

    public class InputValue
    {
        public InputType typ;
        public bool Bool;
        public double Double;

        public InputValue(double value)
        {
            typ = InputType.DOUBLE;
            Double = value;
        }

        public InputValue(bool value)
        {
            typ = InputType.BOOLEAN;
            Bool = value;
        }
    }

    public interface IInputData
    {
        string name { get; }
        bool available { get; }
        InputType typ { get; }
        InputValue value { get; }
    }

    public class Batteries : IInputData
    {
        public string name { get { return "batteries"; } }
        public bool available { get { return TotalElectricChargeCapacity > 0.1f; }}
        public InputType typ {get { return InputType.DOUBLE; } }
        public InputValue value { get { return new InputValue(ElectricChargeFillLevel); } }
        private Vessel parentVessel = null;

        public Batteries (Vessel v)
        {
            parentVessel = v;
        }

        private IEnumerable<PartResource> ElectricChargeResources
        {
            get
            {
                if (this.parentVessel != null && this.parentVessel.rootPart != null) {
                    int ecid = PartResourceLibrary.Instance.GetDefinition ("ElectricCharge").id;
                    List<PartResource> resources = new List<PartResource> ();
                    this.parentVessel.rootPart.GetConnectedResources (ecid, ResourceFlowMode.ALL_VESSEL, resources);
                    return resources;
                }
                return new List<PartResource>();
            }
        }

        private double TotalElectricCharge
        {
            get { return this.ElectricChargeResources.Sum(x => x.amount); }
        }

        private double TotalElectricChargeCapacity
        {
            get { return this.ElectricChargeResources.Sum(x => x.maxAmount); }
        }

        private double ElectricChargeFillLevel
        {
            get { return this.TotalElectricChargeCapacity > 0.1f ? this.TotalElectricCharge / this.TotalElectricChargeCapacity : 0f; }
        }

    }

    public class Gear : IInputData
    {
        public string name { get { return "gear"; } }
        public bool available
        {
            get
            {
                return parentVessel.Parts.Any(p => p.Modules.Contains("ModuleLandingSwitch"));
            }
        }
        public InputType typ { get { return InputType.BOOLEAN; } }
        public InputValue value
        {
            get
            {
                return new InputValue(parentVessel.situation == Vessel.Situations.LANDED);
            }
        }
        private Vessel parentVessel = null;

        public Gear (Vessel v)
        {
            parentVessel = v;
        }
    }

    public class Processor : IDisposable
    {
        public bool hasLevelTrigger, hasLogicOps, hasArithOps;
        int imemWords;
        public List<Instruction> instructions;

        private Part mPart;

        // Warning, may be null
        public Vessel parentVessel { get { return mPart.vessel; } }

        //private ProcessorWindow mWindow;

        private List<IInputData> inputs = new List<IInputData>();

        public Processor (Part part, Modules.ModuleKpuProcessor module)
        {
            mPart = part;
            hasLevelTrigger = module.hasLevelTrigger;
            hasLogicOps = module.hasLogicOps;
            hasArithOps = module.hasArithOps;
            imemWords = module.imemWords;
            instructions = new List<Instruction>();
            inputs.Add(new Batteries(parentVessel));
        }

        private Dictionary<string, InputValue> inputValues;

        public void OnUpdate ()
        {
            inputValues = new Dictionary<string, InputValue>();
            foreach (IInputData i in inputs)
            {
                if (i.available)
                {
                    inputValues.Add(i.name, i.value);
                }
            }
        }

        public void Save (ConfigNode node)
        {
            if (node.HasNode("Processor"))
                node.RemoveNode("Processor");
            
            ConfigNode Proc = new ConfigNode("Processor");

            // TODO store instructions list in Proc

            node.AddNode(Proc);
        }

        public void Load (ConfigNode node)
        {
            if (!node.HasNode("Processor"))
                return;
            
            ConfigNode Proc = node.GetNode("Processor");

            // TODO read instructions list from Proc
        }

        public void Dispose()
        {
            KPU.Logging.Log("Processor: Dispose");

            /*if (mWindow != null)
            {
                mWindow.Hide();
            }*/
        }
    }
}

