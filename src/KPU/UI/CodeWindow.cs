using System;
using System.Collections.Generic;
using UnityEngine;

namespace KPU.UI
{
    public class CodeWindow : AbstractWindow
    {
        public KPU.Processor.Processor mProcessor;
        public List<KPU.Processor.Instruction> instructions;
        public bool mCompiled, mLoaded;
        private string mText;
        private Vector2 mScrollPosition;
        private GUIStyle mHeadingStyle, mBtnStyle, mGreyBtnStyle;

        public CodeWindow (KPU.Processor.Processor processor)
            : base(Guid.NewGuid(), "KPU Code", new Rect(100, 100, 515, 320), WindowAlign.Floating)
        {
            mProcessor = processor;
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
            mGreyBtnStyle = new GUIStyle(mBtnStyle)
            {
                fontStyle = FontStyle.Italic,
            };
            revert();
        }

        private void revert()
        {
            instructions = new List<KPU.Processor.Instruction>(mProcessor.instructions);
            decompile();
            mLoaded = true;
        }

        public void decompile()
        {
            mText = "";
            foreach (KPU.Processor.Instruction i in instructions)
            {
                if (mText.Length > 0) mText += "\n";
                mText += i.ToString();
            }
            mCompiled = true;
        }

        private bool compile()
        {
            instructions = new List<KPU.Processor.Instruction>();
            mLoaded = false;
            int imemWords = 0;
            foreach (string line in mText.Split('\n'))
            {
                if (line.Length == 0) continue;
                KPU.Processor.Instruction i;
                try
                {
                    i = new KPU.Processor.Instruction(line);
                }
                catch (KPU.Processor.Instruction.ParseError exc)
                {
                    Logging.Message("Bad line " + line);
                    Logging.Message(exc.ToString());
                    return false;
                }
                instructions.Add(i);
                imemWords += i.imemWords;
            }
            Logging.Message(string.Format("Output IMEM: {0:D} words", imemWords));
            return true;
        }

        public override void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Width(495), GUILayout.Height(240));
                if (mProcessor == null)
                {
                    GUILayout.Label("I have no Processor!?", mHeadingStyle);
                }
                else
                {
                    GUI.SetNextControlName("kpu_ct");
                    string newText = GUILayout.TextArea(mText, GUILayout.Width(480));
                    if (newText != mText)
                    {
                        mCompiled = false;
                        mLoaded = false;
                        mText = newText;
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Compile!", mCompiled ? mGreyBtnStyle : mBtnStyle, GUILayout.ExpandWidth(false)))
                    {
                        mCompiled = compile();
                        if (!mCompiled)
                        {
                            Logging.Message("Compilation failed");
                        }
                        else // normalise the code
                        {
                            decompile();
                        }
                    }
                    if (GUILayout.Button("Undo", mCompiled ? mGreyBtnStyle : mBtnStyle, GUILayout.ExpandWidth(false)))
                    {
                        decompile();
                    }
                    if (GUILayout.Button("Revert", mLoaded ? mGreyBtnStyle : mBtnStyle, GUILayout.ExpandWidth(false)))
                    {
                        revert();
                    }
                    if (GUILayout.Button("Save", mCompiled ? mBtnStyle : mGreyBtnStyle, GUILayout.ExpandWidth(false)) && mCompiled)
                    {
                        string name = KPUCore.Instance.library.chooseName();
                        if (name != null)
                        {
                            KPU.Library.Program prog = new KPU.Library.Program();
                            prog.name = name;
                            prog.description = String.Format("Saved from {0}", FlightGlobals.ActiveVessel.vesselName);
                            prog.addCode(instructions);
                            KPUCore.Instance.library.putProgram(prog);
                            Logging.Message(String.Format("Saved as {0}", name));
                            UI.LibraryNameWindow lnw = new UI.LibraryNameWindow(name);
                            lnw.Show();
                        }
                    }
                    if (GUILayout.Button("Load", mBtnStyle, GUILayout.ExpandWidth(false)))
                    {
                        KPUCore.Instance.openLibraryWindow(this);
                    }
                    if (GUILayout.Button("Rename", mProcessor != null ? mBtnStyle : mGreyBtnStyle, GUILayout.ExpandWidth(false)) && mProcessor != null)
                    {
                        UI.ProcessorRenameWindow prw = new UI.ProcessorRenameWindow(mProcessor);
                        prw.Show();
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            base.Window(id);
        }
    }
}

