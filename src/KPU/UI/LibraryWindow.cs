using System;
using System.Collections.Generic;
using UnityEngine;

namespace KPU.UI
{
    public class LibraryWindow : AbstractWindow
    {
        private GUIStyle mHeadingStyle, mBtnStyle, mGreyBtnStyle, mSelectionStyle, mGoodNewsStyle, mBadNewsStyle;
        private Vector2 mScrollNames, mScrollDesc, mScrollInputs;
        private string mSelected;
        private CodeWindow mCW;
        public LibraryWindow (CodeWindow cw)
            : base(Guid.NewGuid(), "KPU Program Library", new Rect(cw.Position.xMin, cw.Position.yMax + 5, 600, 320), WindowAlign.Floating)
        {
            mCW = cw;
            mSelected = null;
            mHeadingStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
            };
            mBtnStyle = new GUIStyle(HighLogic.Skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
            };
            mGreyBtnStyle = new GUIStyle(HighLogic.Skin.button)
            {
                fontStyle = FontStyle.Italic,
            };
            mSelectionStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState() { textColor = Color.cyan, }
            };
            mGoodNewsStyle = new GUIStyle(HighLogic.Skin.label)
            {
                normal = new GUIStyleState() { textColor = Color.green, }
            };
            mBadNewsStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState() { textColor = Color.red, }
            };
        }

        private void NameList(ref Library.Program selectedProgram)
        {
            // name list pane
            KPU.Library.Library library = KPUCore.Instance.library;
            mScrollNames = GUILayout.BeginScrollView(mScrollNames, GUILayout.Width(240), GUILayout.Height(280));
            try
            {
                if (library == null)
                {
                    GUILayout.Label("I have no Library!?", mHeadingStyle);
                }
                else if (library.isEmpty())
                {
                    GUILayout.Label("There are no saved programs", mHeadingStyle);
                }
                else
                {
                    foreach(string name in library.programNames())
                    {
                        GUIStyle style = HighLogic.Skin.label;
                        if (name.Equals(mSelected))
                            style = mSelectionStyle;
                        if (GUILayout.Button(name, style, GUILayout.Width(216)))
                            mSelected = name;
                    }
                }
                if (mSelected != null)
                {
                    selectedProgram = library.getProgram(mSelected);
                }
            }
            finally
            {
                GUILayout.EndScrollView();
            }
        }

        private void Buttons(Library.Program selectedProgram)
        {
            KPU.Library.Library library = KPUCore.Instance.library;
            GUILayout.BeginHorizontal();
            try
            {
                bool canLoad = (mCW != null) && (selectedProgram != null);
                if (GUILayout.Button("Load", canLoad ? mBtnStyle : mGreyBtnStyle) && canLoad)
                {
                    mCW.instructions = new List<KPU.Processor.Instruction>(selectedProgram.code);
                    mCW.mLoaded = false;
                    mCW.decompile();
                }
                if (GUILayout.Button("Rename", selectedProgram != null ? mBtnStyle : mGreyBtnStyle) && selectedProgram != null)
                {
                    UI.LibraryRenameWindow lrw = new UI.LibraryRenameWindow(selectedProgram.name);
                    lrw.Show();
                }
                if (GUILayout.Button("Delete", selectedProgram != null ? mBtnStyle : mGreyBtnStyle) && selectedProgram != null)
                {
                    library.deleteProgram(selectedProgram.name);
                }
            }
            finally
            {
                GUILayout.EndHorizontal();
            }
        }

        private void require(string what, bool want, bool have)
        {
            GUILayout.Label(String.Format("Requires {0}: {1}", what, want ? "Yes" : "No"), have || !want ? mGoodNewsStyle : mBadNewsStyle, GUILayout.Width(270));
        }

        private void UsedInputs(Library.Program selectedProgram, Processor.Processor proc)
        {
            GUILayout.Label("Inputs Used", mHeadingStyle, GUILayout.Width(270));
            List<string> usedInputs = new List<string>(selectedProgram.usedInputs);
            usedInputs.Sort();
            foreach (string input in usedInputs)
            {
                if (proc == null || proc.inputValues == null)
                {
                    GUILayout.Label(input, HighLogic.Skin.label, GUILayout.Width(270));
                }
                else
                {
                    bool have = proc.inputValues.ContainsKey(input);
                    GUILayout.Label(input, have ? mGoodNewsStyle : mBadNewsStyle, GUILayout.Width(270));
                }
            }
        }

        private void UsedOrients(Library.Program selectedProgram, Processor.Processor proc)
        {
            GUILayout.Label("Orients Used", mHeadingStyle, GUILayout.Width(270));
            List<string> usedOrients = new List<string>(selectedProgram.usedOrients);
            usedOrients.Sort();
            foreach (string orient in usedOrients)
            {
                if (proc == null)
                {
                    GUILayout.Label(orient, HighLogic.Skin.label, GUILayout.Width(270));
                }
                else
                {
                    bool have = proc.IsOrientationSupported(orient);
                    GUILayout.Label(orient, have ? mGoodNewsStyle : mBadNewsStyle, GUILayout.Width(270));
                }
            }
        }

        private void ProgramDetails(Library.Program selectedProgram)
        {
            // program details pane
            if (selectedProgram == null)
            {
                GUILayout.Label("Select a program from the list", mHeadingStyle, GUILayout.Width(280));
            }
            else
            {
                GUILayout.Label(selectedProgram.name, mHeadingStyle, GUILayout.Width(280));
                mScrollDesc = GUILayout.BeginScrollView(mScrollDesc, GUILayout.Height(64));
                try
                {
                    GUI.SetNextControlName("kpu_dt");
                    selectedProgram.description = GUILayout.TextArea(selectedProgram.description, GUILayout.Width(270));
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
                GUILayout.Label("Requirements", mHeadingStyle, GUILayout.Width(280));
                KPU.Processor.Processor proc = null;
                if (mCW != null)
                    proc = mCW.mProcessor;
                require("Level Trigger", selectedProgram.requiresLevelTrigger, (proc == null) ? false : proc.hasLevelTrigger);
                require("Logic Ops", selectedProgram.requiresLogicOps, (proc == null) ? false : proc.hasLogicOps);
                require("Arithmetic Ops", selectedProgram.requiresArithOps, (proc == null) ? false : proc.hasArithOps);
                bool enough = (proc == null) ? false : selectedProgram.imemWords <= proc.maxImemWords;
                GUILayout.Label(String.Format("Requires {0} words of IMEM", selectedProgram.imemWords), enough ? mGoodNewsStyle : mBadNewsStyle);
                mScrollInputs = GUILayout.BeginScrollView(mScrollInputs, GUILayout.Height(64));
                try
                {
                    UsedInputs(selectedProgram, proc);
                    UsedOrients(selectedProgram, proc);
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }
        }

        public override void Window(int id)
        {
            KPU.Library.Library library = KPUCore.Instance.library;
            KPU.Library.Program selectedProgram = null;
            GUILayout.BeginHorizontal(GUILayout.Height(310));
            try
            {
                GUILayout.BeginVertical(GUILayout.Width(250));
                try
                {
                    NameList(ref selectedProgram);
                    Buttons(selectedProgram);
                }
                finally
                {
                    GUILayout.EndVertical();
                }
                GUILayout.BeginVertical(GUILayout.Width(300));
                try
                {
                    ProgramDetails(selectedProgram);
                }
                finally
                {
                    GUILayout.EndVertical();
                }
            }
            finally
            {
                GUILayout.EndHorizontal();
                base.Window(id);
            }
        }
    }
}

