using System;
using UnityEngine;

namespace KPU.UI
{
    public class ProcessorRenameWindow : AbstractWindow
    {
        private GUIStyle mBtnStyle;
        private string mNewName;
        private KPU.Processor.Processor mProcessor;
        public ProcessorRenameWindow (KPU.Processor.Processor processor)
            : base(Guid.NewGuid(), String.Format("Rename Processor"), new Rect(Screen.width / 2 - 100, Screen.height / 2 - 30, 200, 60), WindowAlign.Floating)
        {
            mProcessor = processor;
            mNewName = processor.name;
            mBtnStyle = new GUIStyle(HighLogic.Skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
            };
        }

        public override void Window(int id)
        {
            bool toclose = false;
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("kpu_nt");
            mNewName = GUILayout.TextField(mNewName, GUILayout.Width(120));
            if (GUILayout.Button("OK", mBtnStyle, GUILayout.Width(48)))
            {
                mProcessor.name = mNewName;
                toclose = true;
            }
            if (GUILayout.Button("Cancel", mBtnStyle, GUILayout.Width(48)))
            {
                toclose = true;
            }
            GUILayout.EndHorizontal();
            if (toclose) Hide();
        }
    }
}