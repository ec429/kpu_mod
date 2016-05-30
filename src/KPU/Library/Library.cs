using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KPU.Library
{
    public class Program
    {
        public string name, description;
        public List<KPU.Processor.Instruction> code;
        public Program()
        {
            name = null;
            description = "";
            code = new List<KPU.Processor.Instruction>();
        }

        public void addCode(List<KPU.Processor.Instruction> insns)
        {
            code.AddRange(insns);
        }

        public bool requiresLevelTrigger { get {
            return code.Exists(i => i.requiresLevelTrigger);
        }}
        public bool requiresLogicOps { get {
            return code.Exists(i => i.requiresLogicOps);
        }}
        public bool requiresArithOps { get {
            return code.Exists(i => i.requiresArithOps);
        }}
        public int imemWords { get {
            return code.Sum(i => i.imemWords);
        }}

        public HashSet<string> usedInputs { get {
            HashSet<string> used = new HashSet<string>(), ignore = new HashSet<string>();
            foreach (KPU.Processor.Instruction i in code)
                i.usedInputs(ref used, ref ignore);
            return used;
        }}
        public HashSet<string> usedOrients { get {
                HashSet<string> used = new HashSet<string>(), ignore = new HashSet<string>();
            foreach (KPU.Processor.Instruction i in code)
                i.usedInputs(ref ignore, ref used);
            return used;
        }}

        public void Load(ConfigNode node)
        {
            if (node.HasValue("programName"))
                name = node.GetValue("programName");
            if (node.HasValue("programDesc"))
                description = Util.unEscapeNewlines(node.GetValue("programDesc"));
            if (node.HasNode("Instructions"))
            {
                ConfigNode iln = node.GetNode("Instructions");
                foreach (string isn in iln.GetValues("code"))
                {
                    KPU.Processor.Instruction insn = new KPU.Processor.Instruction(isn);
                    code.Add(insn);
                }
            }
        }

        public void Save(ConfigNode node)
        {
            node.AddValue("programName", name);
            node.AddValue("programDesc", Util.escapeNewlines(description));
            ConfigNode iln = new ConfigNode("Instructions");
            node.AddNode(iln);
            foreach (KPU.Processor.Instruction i in code)
            {
                iln.AddValue("code", i.mText);
            }
        }
    }

    public class Library
    {
        private Dictionary<string, Program> mPrograms;
        public Library ()
        {
            mPrograms = new Dictionary<string, Program>();
        }

        public bool isEmpty()
        {
            return mPrograms.Count == 0;
        }

        public List<string> programNames()
        {
            List<string> names = new List<string>(mPrograms.Keys);
            names.Sort();
            return names;
        }

        public bool nameExists(string name)
        {
            return mPrograms.ContainsKey(name);
        }

        public string chooseName()
        {
            if (!nameExists("Untitled"))
                return "Untitled";
            for (int i = 2; i != 0; i++)
            {
                string name = String.Format("Untitled #{0:D}", i);
                if (!nameExists(name))
                    return name;
            }
            // Ran out of integers!
            Logging.Message("Ran out of potential names.  Clean up or rename some untitled programs!");
            return null;
        }

        public bool renameProgram(string oldName, string newName)
        {
            Program prog = getProgram(oldName);
            if (prog == null)
            {
                Logging.Message(String.Format("No program {0} to rename", oldName));
                return false;
            }
            if (oldName == newName) // nothing to do
                return true;
            if (nameExists(newName))
            {
                Logging.Message(String.Format("Name {0} is already in use", newName));
                return false;
            }
            mPrograms.Remove(oldName);
            prog.name = newName;
            putProgram(prog);
            return true;
        }

        public Program getProgram(string name)
        {
            if (nameExists(name))
                return mPrograms[name];
            return null;
        }

        public void putProgram(Program prog)
        {
            mPrograms.Add(prog.name, prog);
        }

        public void deleteProgram(string name)
        {
            if (nameExists(name))
                mPrograms.Remove(name);
        }

        public void Load(ConfigNode node)
        {
            foreach (ConfigNode pn in node.GetNodes("PROGRAM"))
            {
                Program prog = new Program();
                prog.Load(pn);
                putProgram(prog);
            }
        }

        public void Save(ConfigNode node)
        {
            foreach (Program prog in mPrograms.Values)
            {
                ConfigNode pn = node.AddNode("PROGRAM");
                prog.Save(pn);
            }
        }
    }
}

