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
    }

    public interface IInputData
    {
        string name { get; }
        bool available { get; }
        double value { get; }
    }

    public class Batteries : IInputData
    {
        public string name { get { return "batteries"; } }
        public bool available { get { return TotalElectricChargeCapacity > 0.1f; }}
        public double value { get { return ElectricChargeFillLevel; } }
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

    public class Processor : IDisposable
    {
        public bool hasLevelTrigger, hasLogicOps, hasArithOps;
        public List<Instruction> instructions;

        private Part mPart;

        // Warning, may be null
        public Vessel parentVessel { get { return mPart.vessel; } }

        //private ProcessorWindow mWindow;

        private List<IInputData> inputs = new List<IInputData>();

        public Processor (Part part, bool level, bool logic, bool arith)
        {
            mPart = part;
            hasLevelTrigger = level;
            hasLogicOps = logic;
            hasArithOps = arith;
            instructions = new List<Instruction>();
            inputs.Add(new Batteries(parentVessel));
        }

        private Dictionary<string, double> inputValues;

        public void OnUpdate ()
        {
            inputValues = new Dictionary<string, double>();
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

