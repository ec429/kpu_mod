using System;
using UnityEngine;

namespace KPU.UI
{
    public class LibraryNameWindow : AbstractWindow
    {
        private GUIStyle mBtnStyle, mGreyBtnStyle;
        private string mOldName, mNewName;
        public LibraryNameWindow (string oldName)
            : base(Guid.NewGuid(), String.Format("Name Program"), new Rect(Screen.width / 2 - 100, Screen.height / 2 - 30, 200, 60), WindowAlign.Floating)
        {
            mOldName = oldName;
            mNewName = oldName;
            mBtnStyle = new GUIStyle(HighLogic.Skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
            };
            mGreyBtnStyle = new GUIStyle(HighLogic.Skin.button)
            {
                fontStyle = FontStyle.Italic,
            };
        }

        public override void Window(int id)
        {
            bool toclose = false;
            KPU.Library.Library library = KPUCore.Instance.library;
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("kpu_nt");
            mNewName = GUILayout.TextField(mNewName, GUILayout.Width(120));
            if (GUILayout.Button("OK", library.nameExists(mNewName) ? mGreyBtnStyle : mBtnStyle, GUILayout.Width(48)))
            {
                toclose = library.renameProgram(mOldName, mNewName);
            }
            GUILayout.EndHorizontal();
            if (toclose) Hide();
        }
    }

    public class LibraryRenameWindow : AbstractWindow
    {
        private GUIStyle mBtnStyle, mGreyBtnStyle;
        private string mOldName, mNewName;
        public LibraryRenameWindow (string oldName)
            : base(Guid.NewGuid(), String.Format("Rename Program '{0}'", oldName), new Rect(Screen.width / 2 - 120, Screen.height / 2 - 30, 240, 60), WindowAlign.Floating)
        {
            mOldName = oldName;
            mNewName = oldName;
            mBtnStyle = new GUIStyle(HighLogic.Skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
            };
            mGreyBtnStyle = new GUIStyle(HighLogic.Skin.button)
            {
                fontStyle = FontStyle.Italic,
            };
        }

        public override void Window(int id)
        {
            bool toclose = false;
            KPU.Library.Library library = KPUCore.Instance.library;
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("kpu_rt");
            mNewName = GUILayout.TextField(mNewName, GUILayout.Width(120));
            if (GUILayout.Button("Rename", library.nameExists(mNewName) ? mGreyBtnStyle : mBtnStyle, GUILayout.Width(48)))
            {
                toclose = library.renameProgram(mOldName, mNewName);
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