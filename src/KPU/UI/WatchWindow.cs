using System;
using UnityEngine;

namespace KPU.UI
{
    public class WatchWindow : AbstractWindow
    {
        KPU.Processor.Processor mProcessor;
        private Vector2 mScrollPosition;
        private GUIStyle mKeyStyle, mValueStyle, mHeadingStyle;

        public WatchWindow (KPU.Processor.Processor processor)
            : base(Guid.NewGuid(), "KPU Watch", new Rect(100, 100, 240, 320), WindowAlign.Floating)
        {
            mProcessor = processor;
            mKeyStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fixedWidth = 100,
                fontStyle = FontStyle.Bold,
                fontSize = 11,
            };
            mValueStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fixedWidth = 96,
                fontSize = 11,
            };
            mHeadingStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fixedWidth = 200,
                fontStyle = FontStyle.Bold,
                fontSize = 14,
            };
        }

        public override void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Width(224), GUILayout.Height(310));
                if (mProcessor == null)
                {
                    GUILayout.Label("I have no Processor!?", mHeadingStyle);
                }
                else if(!mProcessor.hasPower)
                {
                    GUILayout.Label("--NO POWER--", mHeadingStyle);
                }
                else
                {
                    GUILayout.Label("Input Values", mHeadingStyle);
                    if (mProcessor.inputValues != null)
                    {
                        foreach (System.Collections.Generic.KeyValuePair<string, KPU.Processor.Instruction.Value> item in mProcessor.inputValues)
                        {
                            if (item.Key.StartsWith("latch")) continue;
                            if (item.Key.StartsWith("timer")) continue;
                            string unit = "";
                            bool useSI = false;
                            if (mProcessor.inputs.ContainsKey(item.Key))
                            {
                                unit = mProcessor.inputs[item.Key].unit;
                                useSI = mProcessor.inputs[item.Key].useSI;
                            }
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(item.Key, mKeyStyle);
                            if (useSI && item.Value.typ == KPU.Processor.Instruction.Type.DOUBLE)
                                GUILayout.Label(Util.formatSI(item.Value.d, unit), mValueStyle);
                            else
                                GUILayout.Label(item.Value.ToString() + unit, mValueStyle);
                            GUILayout.EndHorizontal();
                        }
                    }
                    if (mProcessor.latches > 0 && mProcessor.latchState != null)
                    {
                        GUILayout.Label("Latch Values", mHeadingStyle);
                        for (int i = 0; i < mProcessor.latches; i++)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(string.Format("latch{0:D}", i), mKeyStyle);
                            GUILayout.Label(mProcessor.latchState[i].ToString(), mValueStyle);
                            GUILayout.EndHorizontal();
                        }
                    }
                    if (mProcessor.timers > 0 && mProcessor.timerState != null)
                    {
                        GUILayout.Label("Timer Values", mHeadingStyle);
                        for (int i = 0; i < mProcessor.timers; i++)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(mProcessor.timerState[i].name, mKeyStyle);
                            GUILayout.Label(mProcessor.timerState[i].value.ToString() + mProcessor.timerState[i].unit, mValueStyle);
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.Label("Output Values", mHeadingStyle);
                    if (mProcessor.outputs != null)
                    {
                        foreach (Processor.IOutputData output in mProcessor.outputs.Values)
                        {
                            if (output.name.StartsWith("latch")) continue;
                            if (output.name.StartsWith("timer")) continue;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(output.name, mKeyStyle);
                            GUILayout.Label(output.value.ToString(), mValueStyle);
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            base.Window(id);
        }
    }
}

