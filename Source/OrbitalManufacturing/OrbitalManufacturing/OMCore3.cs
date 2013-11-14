using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.IO;

namespace OrbitalManufacturing
{
    /*
     * This portion of the OMCore module contains the craft verification and resource adjustment code
     */
    public partial class OMCore : PartModule
    {
        private void drawAdjustGUI()
        {
            uifs.windowPosition = GUILayout.Window(1, uifs.windowPosition, AdjustGUI, "Orbital Manufacturing", GUILayout.MinWidth(140));
        }

        private void AdjustGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (GUILayout.Button("Build Craft"))
            {
                BuildCraft();
                uifs.showcraftbrowser = false;
                uifs.uivisible = false;
                uifs.shipselected = false;
                HideMenu();
                uifs.buildstep = BuildStep.Select;
            }
            if (GUILayout.Button("Close"))
            {
                uifs.showcraftbrowser = false;
                uifs.uivisible = false;
                uifs.shipselected = false;
                HideMenu();
                uifs.buildstep = BuildStep.Select;
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private Transform GetLaunchTransform()
        {

            Transform launchTransform;

            Vector3 offset = Vector3.up;
            Transform t = this.part.transform;
            GameObject launchPos = new GameObject();
            launchPos.transform.localScale = t.localScale;
            launchPos.transform.parent = t;
            launchPos.transform.position = t.position;
            launchPos.transform.position += t.TransformDirection(offset);
            launchPos.transform.rotation = t.rotation;
            launchTransform = launchPos.transform;
            Destroy(launchPos);

            return launchTransform;
        }

        private void BuildCraft()
        {
            // build craft
            ShipConstruct nship = ShipConstruction.LoadShip(uifs.craftfile);
            Transform launchtransform = GetLaunchTransform();

            Vector3 offset = nship.Parts[0].transform.localPosition;
            nship.Parts[0].transform.Translate(-offset);
            Game state = FlightDriver.FlightStateCache;
            VesselCrewManifest crew = new VesselCrewManifest();

            ShipConstruction.CreateBackup(nship);
            ShipConstruction.PutShipToGround(nship, launchtransform);
            ShipConstruction.AssembleForLaunch(nship, uifs.LaunchSiteName, uifs.flagname, state, crew);

            Vessel vsl = FlightGlobals.Vessels[FlightGlobals.Vessels.Count - 1];
            FlightGlobals.ForceSetActiveVessel(vsl);
            vsl.Landed = false;

            HackStruts(vsl);
            uifs.vesselInfo = new DockedVesselInfo();
            uifs.vesselInfo.name = vsl.vesselName;
            uifs.vesselInfo.vesselType = vsl.vesselType;
            uifs.vesselInfo.rootPartUId = vsl.rootPart.uid;
            uifs.vesselFlightID = vsl.rootPart.flightID;
            FlightDriver.flightStarted = true;
            FlightDriver.newShipFlagURL = uifs.flagname;
            FlightDriver.newShipManifest = crew;
            vsl.state = Vessel.State.ACTIVE;
            vsl.situation = Vessel.Situations.ORBITING;
            Staging.beginFlight();
            vsl.ResumeStaging();
            Staging.GenerateStagingSequence(vsl.rootPart);
            Staging.RecalculateVesselStaging(vsl);

            FlightGlobals.ForceSetActiveVessel(vessel);
            vsl.rootPart.Couple(part);
            vessel.ResumeStaging();
            Staging.GenerateStagingSequence(vessel.rootPart);
            Staging.RecalculateVesselStaging(vessel);

            Events[("DecoupleVessel")].active = true;
        }

        private Part GetLastPart(Vessel v, Transform t)
        {
            float dist = Vector3.Distance(t.position, v.rootPart.transform.position);
            Part LastPart = v.rootPart;
            foreach (Part p in v.Parts)
            {
                if (Vector3.Distance(t.position, p.transform.position) < dist)
                {
                    dist = Vector3.Distance(t.position, p.transform.position);
                    LastPart = p;
                }
            }
            return LastPart;
        }

        private void HackStrutCData(Part p, int numParts)
        {
            Debug.Log(String.Format("[EL] before {0}", p.customPartData));
            string[] Params = p.customPartData.Split(';');
            for (int i = 0; i < Params.Length; i++)
            {
                string[] keyval = Params[i].Split(':');
                string Key = keyval[0].Trim();
                string Value = keyval[1].Trim();
                if (Key == "tgt")
                {
                    string[] pnameval = Value.Split('_');
                    string pname = pnameval[0];
                    int val = int.Parse(pnameval[1]);
                    if (val != -1)
                    {
                        val += numParts;
                    }
                    Params[i] = "tgt: " + pname + "_" + val.ToString();
                    break;
                }
            }
            p.customPartData = String.Join("; ", Params);
            Debug.Log(String.Format("[EL] after {0}", p.customPartData));
        }

        private void HackStruts(Vessel vsl)
        {
            int numParts = vessel.parts.Count;

            var struts = vsl.parts.OfType<StrutConnector>().Where(p => p.customPartData != "");
            foreach (Part part in struts)
            {
                HackStrutCData(part, numParts);
            }
            var fuelLines = vsl.parts.OfType<FuelLine>().Where(p => p.customPartData != "");
            foreach (Part part in fuelLines)
            {
                HackStrutCData(part, numParts);
            }
        }
    }
}
