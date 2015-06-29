using System;
using UnityEngine;

namespace KPU.UI
{
    public class WatchWindow : AbstractWindow
    {
        KPU.Processor.Processor mProcessor;
        private Vector2 mScrollPosition;
        private GUIStyle mKeyStyle, mValueStyle;

        public WatchWindow (KPU.Processor.Processor processor)
            : base(Guid.NewGuid(), "KPU Watch", new Rect(100, 100, 160, 320), WindowAlign.Floating)
        {
            mProcessor = processor;
            mKeyStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fixedWidth = 67,
                fontStyle = FontStyle.Bold,
                fontSize = 11,
            };
            mValueStyle = new GUIStyle(mKeyStyle)
            {
                fontStyle = FontStyle.Normal,
            };
        }

        public override void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Width(150), GUILayout.Height(310));
                foreach (System.Collections.Generic.KeyValuePair<string, KPU.Processor.InputValue> item in mProcessor.inputValues)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(item.Key, mKeyStyle);
                    GUILayout.Label(item.Value.ToString(), mValueStyle);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            base.Window(id);
        }
    }
}

