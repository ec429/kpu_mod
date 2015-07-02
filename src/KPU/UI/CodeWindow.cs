using System;
using System.Collections.Generic;
using UnityEngine;

namespace KPU.UI
{
    public class CodeWindow : AbstractWindow
    {
        private KPU.Processor.Processor mProcessor;
        public List<KPU.Processor.Instruction> instructions;
        public bool mCompiled, mLoaded;
        private string mText;
        private Vector2 mScrollPosition;
        private GUIStyle mHeadingStyle, mBtnStyle, mGreyBtnStyle;

        public CodeWindow (KPU.Processor.Processor processor)
            : base(Guid.NewGuid(), "KPU Code", new Rect(100, 100, 520, 320), WindowAlign.Floating)
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

        private void decompile()
        {
            mText = "";
            foreach (KPU.Processor.Instruction i in instructions)
            {
                if (mText.Length > 0) mText += "\n";
                mText += i.mText;
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
                    Logging.Log("Bad line " + line);
                    Logging.Log(exc.ToString());
                    return false;
                }
                instructions.Add(i);
                imemWords += i.imemWords;
            }
            Logging.Log(string.Format("Output IMEM: {0:D} words", imemWords));
            return true;
        }

        public override void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Width(500), GUILayout.Height(240));
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
                            Logging.Log("Compilation failed");
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
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            base.Window(id);
        }
    }
}

