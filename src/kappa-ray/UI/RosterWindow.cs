using System;
using System.Collections.Generic;
using UnityEngine;

namespace kapparay.UI
{
    public class RosterWindow : AbstractWindow
    {
        private Vector2 mScrollPosition;
        private GUIStyle mHeadingStyle, mKeyStyle, mValueStyle;

        public RosterWindow ()
            : base(Guid.NewGuid(), "Kappa-Ray Roster", new Rect(100, 100, 515, 320), WindowAlign.Floating)
        {
            mHeadingStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                stretchWidth = true,
            };
            mKeyStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
            };
            mValueStyle = new GUIStyle(mKeyStyle)
            {
                fontStyle = FontStyle.Italic,
            };
        }

        public override void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Width(320), GUILayout.Height(240));
                if (Core.Instance == null)
                {
                    GUILayout.Label("I have no Core!?", mHeadingStyle);
                }
                else
                {
                    foreach(KeyValuePair<Vessel,List<KerbalTracker>> kvp in Core.Instance.TrackedKerbals())
                    {
                        if (kvp.Value.Count == 0)
                            continue;
                        GUILayout.Label(kvp.Key.vesselName, mHeadingStyle);
                        foreach(KerbalTracker kt in kvp.Value)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(kt.name, mKeyStyle, GUILayout.Width(140));
                            if (kt.hasCancer)
                                GUILayout.Label("Cancer!", mValueStyle);
                            GUILayout.Label(String.Format("Dose {0:F3}", kt.lifetimeDose), mValueStyle);
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

