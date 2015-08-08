using System;
using System.Collections.Generic;
using UnityEngine;

namespace kapparay.UI
{
    public class FluxWindow : AbstractWindow
    {
        private GUIStyle mHeadingStyle, mKeyStyle, mValueStyle;

        public FluxWindow ()
            : base(Guid.NewGuid(), "Kappa-Ray", new Rect(100, 100, 320, 240), WindowAlign.Floating)
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
                fixedWidth = 80,
            };
            mValueStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fontStyle = FontStyle.Normal,
                fontSize = 12,
                fixedWidth = 60,
            };
        }

        public override void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                Vessel active = FlightGlobals.ActiveVessel;
                if (active == null && PlanetariumCamera.fetch.target.type == MapObject.MapObjectType.VESSEL)
                    active = PlanetariumCamera.fetch.target.vessel;
                RadiationTracker rt = Core.Instance.getRT(active);
                if (!object.ReferenceEquals(rt, null))
                {
                    GUILayout.Label("Kappa-Ray Fluxes", mHeadingStyle);
                    List<KeyValuePair<string, double>> rows = new List<KeyValuePair<string, double>>();
                    rows.Add(new KeyValuePair<string, double>("Van Allen:", rt.lastV));
                    rows.Add(new KeyValuePair<string, double>("Solar:", rt.lastS));
                    rows.Add(new KeyValuePair<string, double>("Galactic:", rt.lastG));
                    foreach(KeyValuePair<string, double> kvp in rows)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(kvp.Key, mKeyStyle);
                        GUILayout.Label(String.Format("{0:F3}", kvp.Value), mValueStyle);
                        GUILayout.EndHorizontal();
                    }
                }
                if (GUILayout.Button("Show Roster"))
                    Core.Instance.ShowRoster();
            }
            GUILayout.EndVertical();
            base.Window(id);
        }
    }
}

