﻿using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.UI;
using KSP.Localization;
using System.Reflection;

namespace MassChart
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class MassChart : MonoBehaviour
    {
        private Vector2 scrollPosition;
        private bool windowVisible;
        private bool stylesConfigured;
        private GUIStyle windowStyle;
        private GUIStyle labelStyle;

        private int windowID = 0;
        private Rect windowRect = new Rect(10, 10, 400, 300);

        private ApplicationLauncherButton appButton; //

        private const float ResizeHandleSize = 16f;
        private Vector2 minWindowSize = new Vector2(400, 300);




        private enum SortMode
        {
            Order,
            Mass,
            Aggregate
        }

        private bool isSortDescending = true;

        //private Dictionary<string, float> AggregatePartsByCategory(List<Part> parts)
        private Dictionary<string, (float Mass, int Count)> AggregatePartsByCategory(List<Part> parts)
        {
            Dictionary<string, (float Mass, int Count)> aggregatedParts = new Dictionary<string, (float Mass, int Count)>();

            foreach (Part p in parts)
            {
                string partName = p.partInfo.title;
                float partMass = p.mass + p.GetResourceMass();

                if (aggregatedParts.ContainsKey(partName))
                {
                    aggregatedParts[partName] = (aggregatedParts[partName].Mass + partMass, aggregatedParts[partName].Count + 1);
                }
                else
                {
                    aggregatedParts[partName] = (partMass, 1);
                }
            }

            return aggregatedParts;
        }

        private void SortPartsByMass(ref List<Part> parts)
        {
            if (isSortDescending)
            {
                parts.Sort((p1, p2) => (p2.mass + p2.GetResourceMass()).CompareTo(p1.mass + p1.GetResourceMass()));
            }
            else
            {
                parts.Sort((p1, p2) => (p1.mass + p1.GetResourceMass()).CompareTo(p2.mass + p2.GetResourceMass()));
            }
        }

        private SortMode currentSortMode = SortMode.Order;

        private void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIApplicationLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIApplicationLauncherDestroyed);
        }

        private void OnGUIApplicationLauncherReady()
        {
            //ApplicationLauncher.Instance.AddModApplication(ShowWindow, HideWindow, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, GetIcon());
            if (appButton == null)
            {
                appButton = ApplicationLauncher.Instance.AddModApplication(ShowWindow, HideWindow, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, GetIcon());
            }
        }

        private void RemoveAppButton()
        {
            if (appButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(appButton);
                appButton = null;
            }
        }

        private void OnGUIApplicationLauncherDestroyed()
        {
            //if (windowVisible) HideWindow();
            if (windowVisible) HideWindow();
            RemoveAppButton();
        }

        private void ShowWindow()
        {
            windowVisible = true;
        }

        private void HideWindow()
        {
            windowVisible = false;
        }

        private void OnGUI()
        {
            if (!windowVisible) return;

            if (!stylesConfigured)
            {
                ConfigureStyles();
            }

            windowRect = GUILayout.Window(windowID, windowRect, DrawWindow, "Vessel Mass", windowStyle);
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sort by Order"))
            {
                if (currentSortMode == SortMode.Order)
                {
                    isSortDescending = !isSortDescending;
                }
                else
                {
                    isSortDescending = true;
                }
                currentSortMode = SortMode.Order;
            }

            if (GUILayout.Button("Sort by Mass"))
            {
                if (currentSortMode == SortMode.Mass)
                {
                    isSortDescending = !isSortDescending;
                }
                else
                {
                    isSortDescending = true;
                }
                currentSortMode = SortMode.Mass;
            }
            if (GUILayout.Button("Aggregate"))
            {
                currentSortMode = SortMode.Aggregate;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Total Vessel Mass: " + EditorLogic.fetch.ship.GetTotalMass().ToString("F2") + " tons", labelStyle);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(380), GUILayout.Height(200));

            List<Part> sortedParts = EditorLogic.fetch.ship.Parts.OrderBy(p => p.transform.position.y).Reverse().ToList();

            if (currentSortMode == SortMode.Order)
            {
                if (isSortDescending)
                {
                    sortedParts = EditorLogic.fetch.ship.Parts.OrderByDescending(p => p.transform.position.y).ToList();
                }
                else
                {
                    sortedParts = EditorLogic.fetch.ship.Parts.OrderBy(p => p.transform.position.y).ToList();
                }
            }


            if (currentSortMode == SortMode.Mass)
            {
                SortPartsByMass(ref sortedParts);
            }

            if (currentSortMode == SortMode.Aggregate)
            {
                Dictionary<string, (float Mass, int Count)> aggregatedParts = AggregatePartsByCategory(sortedParts);

                foreach (KeyValuePair<string, (float Mass, int Count)> kvp in aggregatedParts)
                {
                    string partName = kvp.Key;
                    float partMass = kvp.Value.Mass;
                    int partCount = kvp.Value.Count;
                    float partMassPercentage = (partMass / EditorLogic.fetch.ship.GetTotalMass()) * 100;

                    GUILayout.Label($"{partName} ({partCount}): {partMass.ToString("F2")} tons ({partMassPercentage.ToString("F2")}%)", labelStyle);
                }
            }
            else
            {
                Dictionary<string, int> partCounts = new Dictionary<string, int>();

                foreach (Part p in sortedParts)
                {
                    string partName = p.partInfo.title;
                    if (!partCounts.ContainsKey(partName))
                    {
                        partCounts[partName] = 1;
                    }
                    else
                    {
                        partCounts[partName]++;
                    }

                    string indexedPartName = partName + " #" + partCounts[partName];
                    float partMass = p.mass + p.GetResourceMass();
                    float partMassPercentage = (partMass / EditorLogic.fetch.ship.GetTotalMass()) * 100;

                    GUILayout.Label($"{indexedPartName}: {partMass.ToString("F2")} tons ({partMassPercentage.ToString("F2")}%)", labelStyle);
                }
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }


        private void ConfigureStyles()
        {
            windowStyle = new GUIStyle(HighLogic.Skin.window)
            {
                padding = new RectOffset(10, 10, 25, 10),
                fontSize = 12
            };

            labelStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 12
            };

            stylesConfigured = true;
        }

        private Texture2D GetIcon()
        {
            string iconPath = "MassChart/Textures/toolbar_icon";
            return GameDatabase.Instance.GetTexture(iconPath, false);
        }

        private void OnDestroy()
        {
            //GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIApplicationLauncherReady);
            //GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIApplicationLauncherDestroyed);
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIApplicationLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIApplicationLauncherDestroyed);
            RemoveAppButton();
        }
    }

}