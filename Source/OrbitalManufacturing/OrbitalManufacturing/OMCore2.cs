using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.IO;

namespace OrbitalManufacturing
{
    /*
     * This portion of the OMCore module contains the code for craft selection/verification
     */

    public partial class OMCore : PartModule
    {
        private void drawCraftSelectGUI()
        {
            GuiUtils.LoadSkin(GuiUtils.SkinType.Default);
            uifs.windowPosition = GUILayout.Window(1, uifs.windowPosition, CraftSelectGUI, "Orbital Manufacturing", GUILayout.MinWidth(140));
        }

        private void CraftSelectGUI(int windowID)
        {
            EditorLogic editor = EditorLogic.fetch;
            if (editor) return;
            if (!uifs.uivisible) return;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();
            // VAB / SPH selection
            if (GUILayout.Toggle(uifs.ctype == crafttype.VAB, "VAB", GUILayout.Width(80)))
            {
                uifs.ctype = crafttype.VAB;
            }
            if (GUILayout.Toggle(uifs.ctype == crafttype.SPH, "SPH", GUILayout.Width(80)))
            {
                uifs.ctype = crafttype.SPH;
            }
            if (GUILayout.Toggle(uifs.ctype == crafttype.SUB, "SubAss", GUILayout.Width(160)))
            {
                uifs.ctype = crafttype.SUB;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            string strpath = HighLogic.SaveFolder;

            if (GUILayout.Button("Select Craft", GuiUtils.basicbutton, GUILayout.ExpandWidth(true)))
            {
                string[] dir = new string[] { "SPH", "VAB", "../Subassemblies" };
                bool stock = HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels;
                if (uifs.ctype == crafttype.SUB)
                    HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = false;
                uifs.craftlist = new CraftBrowser(new Rect(Screen.width / 2, 100, 350, 500), dir[(int)uifs.ctype], strpath, "Select a ship to load", craftSelectComplete, craftSelectCancel, HighLogic.Skin, EditorLogic.ShipFileImage, true);
                uifs.showcraftbrowser = true;
                HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels = stock;
            }
            if (uifs.shipselected)
            {
                RenderingManager.RemoveFromPostDrawQueue(1, new Callback(drawCraftSelectGUI)); //stop the GUI
                uifs.buildstep = BuildStep.Adjust;
                
                RenderingManager.AddToPostDrawQueue(1, new Callback(drawAdjustGUI));//start the GUI
            }

            if (GUILayout.Button("Close"))
            {
                uifs.showcraftbrowser = false;
                uifs.uivisible = false;
                uifs.shipselected = false;
                HideMenu();
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        //From ExLaunchPads
        // called when the user selects a craft the craft browser
        private void craftSelectComplete(string filename, string flagname)
        {
            uifs.showcraftbrowser = false;
            uifs.craftfile = filename;
            uifs.flagname = flagname;
            uifs.craftnode = ConfigNode.Load(filename);
            ConfigNode[] nodes = uifs.craftnode.GetNodes("PART");

            // Get list of resources required to build vessel
            if ((uifs.requiredresources = getBuildCost(nodes)) != null)
                uifs.shipselected = true;
        }

        // called when the user clicks cancel in the craft browser
        private void craftSelectCancel()
        {
            uifs.showcraftbrowser = false;
            uifs.uivisible = false;
            uifs.shipselected = false;
        }

        public Dictionary<string, double> getBuildCost(ConfigNode[] nodes)
        {
            float mass = 0.0f;
            Dictionary<string, double> resources = new Dictionary<string, double>();
            Dictionary<string, double> hull_resources = new Dictionary<string, double>();
            Dictionary<string, bool> missing_parts = new Dictionary<string, bool>();

            foreach (ConfigNode node in nodes)
            {
                string part_name = node.GetValue("part");
                part_name = part_name.Remove(part_name.LastIndexOf("_"));
                AvailablePart ap = PartLoader.getPartInfoByName(part_name);
                if (ap == null)
                {
                    missing_parts[part_name] = true;
                    continue;
                }
                Part p = ap.partPrefab;
                mass += p.mass;
                foreach (PartResource r in p.Resources)
                {
                    if (r.resourceName == "IntakeAir" || r.resourceName == "KIntakeAir")
                    {
                        // Ignore intake Air
                        continue;
                    }

                    Dictionary<string, double> res_dict = resources;

                    PartResourceDefinition res_def;
                    res_def = PartResourceLibrary.Instance.GetDefinition(r.resourceName);
                    if (res_def.resourceTransferMode == ResourceTransferMode.NONE
                        || res_def.resourceFlowMode == ResourceFlowMode.NO_FLOW)
                    {
                        res_dict = hull_resources;
                    }

                    if (!res_dict.ContainsKey(r.resourceName))
                    {
                        res_dict[r.resourceName] = 0.0;
                    }
                    res_dict[r.resourceName] += r.maxAmount;
                }
            }
            if (missing_parts.Count > 0)
            {
                MissingPopup(missing_parts);
                return null;
            }

            // RocketParts for the hull is a separate entity to RocketParts in
            // storage containers
            PartResourceDefinition rp_def;
            rp_def = PartResourceLibrary.Instance.GetDefinition("RocketParts");
            uifs.hullRocketParts = mass / rp_def.density;

            // If non pumpable resources are used, convert to RocketParts
            foreach (KeyValuePair<string, double> pair in hull_resources)
            {
                PartResourceDefinition res_def;
                res_def = PartResourceLibrary.Instance.GetDefinition(pair.Key);
                double hull_mass = pair.Value * res_def.density;
                double hull_parts = hull_mass / rp_def.density;
                uifs.hullRocketParts += hull_parts;
            }

            // If there is JetFuel (ie LF only tanks as well as LFO tanks - eg a SpacePlane) then split off the Surplus LF as "JetFuel"
            if (resources.ContainsKey("Oxidizer") && resources.ContainsKey("LiquidFuel"))
            {
                double jetFuel = 0.0;
                // The LiquidFuel:Oxidizer ratio is 9:11. Try to minimize rounding effects.
                jetFuel = (11 * resources["LiquidFuel"] - 9 * resources["Oxidizer"]) / 11;
                if (jetFuel < 0.01)
                {
                    // Forget it. not getting far on that. Any discrepency this
                    // small will be due to precision losses.
                    jetFuel = 0.0;
                }
                resources["LiquidFuel"] -= jetFuel;
                resources["JetFuel"] = jetFuel;
            }

            return resources;
        }

        private void MissingPopup(Dictionary<string, bool> missing_parts)
        {
            string text = "";
            foreach (string mp in missing_parts.Keys)
                text += mp + "\n";
            int ind = uifs.craftfile.LastIndexOf("/") + 1;
            string craft = uifs.craftfile.Substring(ind);
            craft = craft.Remove(craft.LastIndexOf("."));
            PopupDialog.SpawnPopupDialog("Sorry", "Can't build " + craft + " due to the following missing parts\n\n" + text, "OK", false, GuiUtils.skin);
        }
    }
}
