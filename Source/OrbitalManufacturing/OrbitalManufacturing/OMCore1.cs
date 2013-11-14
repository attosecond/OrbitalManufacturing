using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.IO;

namespace OrbitalManufacturing
{
    /*
     * This portion of the OMCore module contains events, callbacks, variables, and enums/classes
     */

    public enum crafttype { SPH, VAB, SUB };
    public enum DockState { Idle, Paused, Building, Completed };
    public enum BuildStep { Select, Adjust, Build };

    public partial class OMCore : PartModule
    {
        #region Variables etc
        public class UIFlowState
        {
            public bool uivisible = false;
            public bool shipselected = false;
            public bool showcraftbrowser = true;
            public DockState dockstate = DockState.Idle;
            public BuildStep buildstep = BuildStep.Select;
            public crafttype ctype = crafttype.VAB;
            public CraftBrowser craftlist = null;
            public string craftfile = null;
            public string flagname = null;
            public string LaunchSiteName = "Orbital Manufactory";
            public ConfigNode craftnode = null;
            public Rect windowPosition = new Rect();
            public Rect defaultWindowPosition = new Rect();
            public Dictionary<string, double> requiredresources = null;
            public Dictionary<string, bool> missing_parts = null;
            public double hullRocketParts = 0.0;

            public DockedVesselInfo vesselInfo;
            public uint vesselFlightID;
        }

        UIFlowState uifs = new UIFlowState();

        //[KSPField]
        //public uint LastPartID;
        #endregion

        #region Event hooks and helper functions
        [KSPAction("Show Menu")]
        public void EnableMenuAction(KSPActionParam param)
        {
            ShowMenu();
        }

        [KSPAction("Hide Menu")]
        public void DisableMenuAction(KSPActionParam param)
        {
            HideMenu();
        }

        [KSPAction("Decouple")]
        public void DecoupleVesselAction(KSPActionParam param)
        {
            DecoupleVessel();
        }

        [KSPEvent(guiActive = true, guiName = "Show Menu", active = true)]
        public void ShowMenu()
        {
            uifs.uivisible = true;
            Events["ShowMenu"].active = false;
            Events["HideMenu"].active = true;
            switch (uifs.dockstate)
            {
                case DockState.Idle:
                    uifs.windowPosition = uifs.defaultWindowPosition;
                    RenderingManager.AddToPostDrawQueue(1, new Callback(drawCraftSelectGUI));//start the GUI
                    break;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Hide Menu", active = false)]
        public void HideMenu()
        {
            uifs.uivisible = false;
            Events["ShowMenu"].active = true;
            Events["HideMenu"].active = false;
            switch (uifs.dockstate)
            {
                case DockState.Idle:
                    if (uifs.buildstep == BuildStep.Select)
                        RenderingManager.RemoveFromPostDrawQueue(1, new Callback(drawCraftSelectGUI)); //stop the GUI
                    else if (uifs.buildstep == BuildStep.Adjust)
                        RenderingManager.RemoveFromPostDrawQueue(1, new Callback(drawAdjustGUI)); //stop the GUI
                    uifs.defaultWindowPosition = uifs.windowPosition;
                    uifs.windowPosition = new Rect();
                    break;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Decouple", active = false)]
        public void DecoupleVessel()
        {
            vessel.Parts.Find(p => p.uid == uifs.vesselInfo.rootPartUId).Undock(uifs.vesselInfo);
            uifs.vesselInfo = null;
            Events[("DecoupleVessel")].active = false;
            //Staging.GenerateStagingSequence(vessel.rootPart);
            //Staging.RecalculateVesselStaging(vessel);
            //Staging.beginFlight();

            //print(FlightGlobals.Vessels.Count);
            //FlightGlobals.ForceSetActiveVessel(FlightGlobals.Vessels[FlightGlobals.Vessels.Count - 1]);
            //FlightGlobals.ForceSetActiveVessel(vessel);
            //QuickSaveLoad.QuickSave();
            //FlightGlobals.DontDestroyOnLoad(FlightGlobals.FindPartByID(uifs.vesselFlightID).vessel.gameObject);
            
            //HighLogic.LoadScene(GameScenes.FLIGHT);
            FlightGlobals.ForceSetActiveVessel(FlightGlobals.FindPartByID(uifs.vesselFlightID).vessel);
        }
        #endregion

        #region Callbacks
        //From ExLaunchPads
        private void OnGUI()
        {
            if (uifs.showcraftbrowser && uifs.craftlist != null)
            {
                GUI.skin = HighLogic.Skin;
                uifs.craftlist.OnGUI();
                GUI.skin = GuiUtils.skin;
            }
        }
        #endregion

        #region Overrides
        public override void OnSave(ConfigNode node)
        {
            //if (uis.vesselInfo != null)
            //{
            //    uis.vesselInfo.Save(node.AddNode("DockedVesselInfo"));
            //}

            PluginConfiguration config = PluginConfiguration.CreateForType<OMCore>();
            config.SetValue("Window Position", uifs.defaultWindowPosition);
            //config.SetValue("Show Build Menu on StartUp", uis.showbuilduionload);
            config.save();
        }

        public override void OnLoad(ConfigNode node)
        {
            //LoadConfig();
            PluginConfiguration config = PluginConfiguration.CreateForType<OMCore>();
            config.load();
            uifs.defaultWindowPosition = config.GetValue<Rect>("Window Position");
        }
        private void LoadConfig()
        {
            
        }
        #endregion
    }
}
