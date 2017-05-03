#region pre-script
    using System;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using VRageMath;
    using VRage.Game;
    using Sandbox.ModAPI.Interfaces;
    using Sandbox.ModAPI.Ingame;
    using Sandbox.Game.EntityComponents;
    using VRage.Game.Components;
    using VRage.Collections;
    using VRage.Game.ObjectBuilders.Definitions;
    using VRage.Game.ModAPI.Ingame;
    using SpaceEngineers.Game.ModAPI.Ingame;

    namespace IngameScript
    {
        public class Program : MyGridProgram
    {
#endregion
    #region Variables
        // Coniguration:                
        int DefaultRadius = 5000;
        string OriginType;
        string OriginComm;
        string DroneStatus;

        GPSlocation Origin = null;
        GPSlocation Current = null;
        GPSlocation Previous = null;
                
        // Utility                
        int runCount;
        string terminalData;
        string newTerminalData;
        bool DEBUG = false; // Displays Debug Info in LCDMain if Enabled
        bool LCD_DEBUG = false;  // Displays LCD Debug Info in LCDMain if Enabled
        bool AUTOCONFIG = true;  // Will Automatically Configure Block Storage/Information if enabled
        public Dictionary<double, int> lcdSettings = new Dictionary<double, int>(){
        {0.5,52},{0.75,34},{1,25},{2,12}
        };

        int initProgress = 0;

        // Block References          
        IMyRemoteControl remote;
        IMyTextPanel lcdMain;
        List<IMyGyro> blackbox = new List<IMyGyro>();
        IMyLaserAntenna comm;
        List<IMyTextPanel> lcds = new List<IMyTextPanel>();
        public Dictionary<string, IMySensorBlock> sensors = new Dictionary<string, IMySensorBlock>(){ { "asteriod", null }, {"ship", null}, { "player", null } };
        List<string> errorLog = new List<string>();
        List<string> eventLog = new List<string>();

        // Navigation                 
        List<GPSlocation> knownCoords = new List<GPSlocation>();
        List<GPSlocation> testedLocations = new List<GPSlocation>();
        List<GPSlocation> poi = new List<GPSlocation>();  // Script will keep coordinates of locations it finds along the way       

        // AI Variables          
        int coordSpacing = 200;
        int[] genCoordFitness = new int[6]; // [1: Random Coordinate - 2: Inverted Coordinate 3:Vector Addition - 4:Vector Dot Product - 5: Vector Cross Product - 6: Points of Interest]              
        int attempts = 0;
    #endregion

        public void Main(string argument)
        {

            AIModule AI = new AIModule(); initProgress++;
            bool rem = getRemote();
            getPreferences(AI);
            //Storage = "<Massive unknown object^{X:42613 Y:147788 Z:-129714}^0^0>\r\n<Unknown object^{X:35399 Y:142334 Z:-134776}^0^0>\r\n<Massive Asteroid^{X:39759 Y:151628 Z:-130483}^0^0>";          

            //  Main Code //              
            try
            {
                if (setVariables() && rem)
                {
                    if (DEBUG) { Echo("46 - setVariables = true"); writeToLine(lcdMain, "47 - setVariables = true", true); }

                    // Get Status and Respond           
                    switch (DroneStatus)
                    {
                        case "Idle":
                            // Check for Comm Connection -> Send Data if available            
                            // Set Status -> Tranasmitting Data           
                            // Create new waypoint -> Check records for waypoint -> if new waypoint, investigate           
                            Current = newCoordinate(AI);
                            DroneStatus = "Exploring";
                            
                            remote.AddWaypoint(Current.name, Current.gps);
                            remote.SetAutoPilotEnabled(true);

                            break;
                        case "Exploring":
                            // Autopilot enabled and going to destination           

                            break;
                        default:
                            DroneStatus = "Idle";
                            break;
                    }
                    string time = "Current Time:" + DateTime.Now.ToString();
                    string[] header = { "Runtime Count", "Error Count", "AI Status" };
                    string[] newOutput = { runCount.ToString(), errorLog.Count.ToString(), DroneStatus };
                    writeToLine(lcdMain, time, true);
                    writeToLine(lcdMain, evenTextSpace(header, lcdMain), true);
                    writeToLine(lcdMain, matchTextSpace(header, newOutput, lcdMain), true);

                }
                else
                {
                    Echo("Error Initilizing");
                    if (DEBUG) { }
                }
            }
            catch (Exception e)
            {
                exceptionHandler(e,115);
                foreach (string str in eventLog)
                {
                    if (lcdMain != null)
                    {
                        writeToLine(lcdMain, str, true);
                    }
                }
            }
            // Runtime End //              

            //foreach(string str in errorLog){Echo(str);}            
            try
            {
                string updatePrefs =
                    "* Preferences: * \r\n"
                    + "Operating Radius|" + DefaultRadius + "\r\n"
                    + "OriginGPS|" + Origin.ToString() + "\r\n"
                    + "DroneStatus|" + DroneStatus + "\r\n"
                    + "RuntimeCount|" + runCount + "\r\n"
                    + "AIAttempts|" + attempts + "\r\n"
                    + "AICoordinateSpacing|" + coordSpacing + "\r\n"
                    + "AICalcFitness|"
                    + String.Format("{0}*{1}*{2}*{3}*{4}*{5}", AI.aiFitness[0], AI.aiFitness[1], AI.aiFitness[2], AI.aiFitness[3], AI.aiFitness[4], AI.aiFitness[5]);

                Me.CustomData = updatePrefs;
            }
            catch (Exception e) { 
                if(DEBUG) { exceptionHandler(e,128); }
            }
        }

    #region Utility Methods 
        public bool getRemote()
        {
            List<IMyTerminalBlock> l0 = new List<IMyTerminalBlock>(); // Remote Control
            try
            {
                GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(l0);
                remote = (IMyRemoteControl)l0[0];
                initProgress++;
                if (remote == null) { errorLog.Add("Error: Missing Remote Control - \r\n Please Add a Remote Control To Ship\r\n"); return false; } else { return true; }
            }
            catch (Exception e)
            {
                Echo(e.ToString().Split(':')[0].Split('.')[1]);
                return false;
            }
        }

        public bool setVariables()
        {
            List<IMyTerminalBlock> l1 = new List<IMyTerminalBlock>(); // LCD Pannels                
            List<IMyTerminalBlock> l2 = new List<IMyTerminalBlock>(); // Laser Antenna            
            List<IMyTerminalBlock> l3 = new List<IMyTerminalBlock>(); // Gyroscopes            
            List<IMyTerminalBlock> l4 = new List<IMyTerminalBlock>(); // Sensors          
            terminalData = Storage;

            // Set Variables                            
            // Main LCD:    
                try
                {
                    lcdMain = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCDMain");
                    if (lcdMain == null) { errorLog.Add("Error: Missing LCDStatus - \r\n Please Add LCD Panel Named LCDMain to Ship\r\n"); return false; }
                    writeToLCD(lcdMain, "", false);
                    eventLog.Add("167 - Initialize lcdMain");
                }
                catch (Exception e)
                {
                    Echo(e.ToString().Split(':')[0].Split('.')[1]);
                    return false;
                }
            // Other LCDs:            
                /*            
                    Status - [lcdStatus] General information about drone            
                    Remote Status - [lcdRemote] - Status of the Remote Control (Autopilot On/Off - Waypoints Set)            
                    Fuel Status - [lcdFuel] - Status of fuel systems            
                    ?Damage Status - [lcdDamage] - Reports on any ship damage            
                    Data Package Info - [lcdData] - Shows the data currently packaged and awaiting to be sent back to base through a laser antenna and             
                        inter-grid connections            
                
                    Input - [lcdInput] - Will display fields in customData and Public Text.  The user can update the custom data            
                */
                GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(l1);
                foreach (IMyTextPanel txt in l1)
                {
                    string output = "";
                    writeToLCD(txt, "", false);
                    //txt.SetValue("FontSize", (float)0.5);            
                    string[] customData = txt.CustomData.Split('\n');

                    if (LCD_DEBUG) { writeToLine(lcdMain, ("145 - LCD Panel (txt) = " + txt.CustomName), true); }

                    if (txt.CustomName.Contains("[lcdStatus]")) { output += drawLCDStatus(txt); }
                    if (txt.CustomName.Contains("[lcdRemote]")) { output += drawLCDRemote(txt); }
                    if (txt.CustomName.Contains("[lcdFuel]")) { output += drawLCDFuel(txt); }
                    if (txt.CustomName.Contains("[lcdDamage]")) { output += drawLCDDamage(txt); }
                    if (txt.CustomName.Contains("[lcdData]")) { output += drawLCDData(txt); }

                    try
                    {
                        writeToLCD(txt, output, true);
                    }
                    catch (Exception e)
                    {
                        writeToLine(lcdMain, exceptionHandler(e, 156), true);
                    }
                }

                initProgress++;
            // BlackBox Storage in Gyroscopes            
                GridTerminalSystem.GetBlocksOfType<IMyGyro>(l3);

                if (l3.Count != 0)
                {
                    for (int i = 0; i < l3.Count; i++)
                    {
                        string storageData = l3[i].CustomData;
                        string[] sData = storageData.Split('\n')[0].Split(':');

                        writeToLine(lcdMain, storageData, true);

                        if (!blackbox.Contains((IMyGyro)l3[i]))
                        {
                            blackbox.Add((IMyGyro)l3[i]);
                            l3[i].CustomData = "";
                        }
                    }
                    initProgress++;
                    if (DEBUG) { writeToLine(lcdMain,("Gyroscopes Added: " + blackbox.Count),true); }
                }
                else { return false; }

            // Laser Antennas            
                //GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(l2);           
                //comm = (IMyLaserAntenna)l2[0];            

            // Sensors
                GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(l4);
                foreach (IMyTerminalBlock s in l4)
                {
                    string sensorName = s.CustomName;
                    switch (sensorName)
                    {
                        case "Sensor [asteriod]":
                            if (sensors["asteriod"] == null) {
                                s.CustomName = "Sensor [asteriod]";
                                s.SetValue("OnOff", false);
                                s.ApplyAction("Detect Players_Off");
                                s.ApplyAction("Detect Floating Objects_Off");
                                s.ApplyAction("Detect Small Ships_Off");
                                s.ApplyAction("Detect Large Ships_Off");
                                s.ApplyAction("Detect Stations_Off");
                                s.ApplyAction("Detect Asteroids_On");
                                sensors["asteriod"] = (IMySensorBlock)s;
                            }
                            break;
                        case "Sensor [ship]":
                            if (sensors["ship"] == null) {
                                s.CustomName = "Sensor [ship]";
                                s.SetValue("OnOff", false);
                                s.ApplyAction("Detect Players_Off");
                                s.ApplyAction("Detect Floating Objects_Off");
                                s.ApplyAction("Detect Small Ships_On");
                                s.ApplyAction("Detect Large Ships_On");
                                s.ApplyAction("Detect Stations_Off");
                                s.ApplyAction("Detect Asteroids_Off");
                                sensors["ship"] = (IMySensorBlock)s;
                            }
                            break;
                        case "Sensor [player]":
                            if(sensors["player"] == null) {
                                s.CustomName = "Sensor [player]";
                                s.SetValue("OnOff", false);
                                s.ApplyAction("Detect Players_On");
                                s.ApplyAction("Detect Floating Objects_Off");
                                s.ApplyAction("Detect Small Ships_Off");
                                s.ApplyAction("Detect Large Ships_Off");
                                s.ApplyAction("Detect Stations_Off");
                                s.ApplyAction("Detect Asteroids_Off");
                                sensors["player"] = (IMySensorBlock)s;
                            }
                            break;
                        default:
                            if (AUTOCONFIG)
                            {
                                if (sensors["asteriod"] == null)
                                {
                                    s.CustomName = "Sensor [asteriod]";
                                    s.SetValue("OnOff", false);
                                    s.ApplyAction("Detect Players_Off");
                                    s.ApplyAction("Detect Floating Objects_Off");
                                    s.ApplyAction("Detect Small Ships_Off");
                                    s.ApplyAction("Detect Large Ships_Off");
                                    s.ApplyAction("Detect Stations_Off");
                                    s.ApplyAction("Detect Asteroids_On");
                                    sensors["asteriod"] = (IMySensorBlock)s;
                                }
                                else if (sensors["ship"] == null)
                                {
                                    s.CustomName = "Sensor [ship]";
                                    s.SetValue("OnOff", false);
                                    s.ApplyAction("Detect Players_Off");
                                    s.ApplyAction("Detect Floating Objects_Off");
                                    s.ApplyAction("Detect Small Ships_On");
                                    s.ApplyAction("Detect Large Ships_On");
                                    s.ApplyAction("Detect Stations_Off");
                                    s.ApplyAction("Detect Asteroids_Off");
                                    sensors["ship"] = (IMySensorBlock)s;
                                }
                                else if (sensors["player"] == null)
                                {
                                    s.CustomName = "Sensor [player]";
                                    s.SetValue("OnOff", false);
                                    s.ApplyAction("Detect Players_On");
                                    s.ApplyAction("Detect Floating Objects_Off");
                                    s.ApplyAction("Detect Small Ships_Off");
                                    s.ApplyAction("Detect Large Ships_Off");
                                    s.ApplyAction("Detect Stations_Off");
                                    s.ApplyAction("Detect Asteroids_Off");
                                    sensors["player"] = (IMySensorBlock)s;
                                }
                                else if (s.CustomName != "Sensor" || !s.CustomName.Contains("[]"))
                                {
                                    s.CustomName = "Sensor [extra]";
                                }
                            }
                            break;
                    }
                }

            // Get Known Data           
                string[] rawData = terminalData.Split('\n');
                foreach (string str in rawData)
                {
                    GPSlocation gps = new GPSlocation(str); bool compare = false;

                    foreach (GPSlocation g in knownCoords)
                    {
                        if (gps.ToString() == g.ToString()) { compare = true; }
                    }

                    if (!compare) { knownCoords.Add(gps); writeToLine(lcdMain, ("Added: " + str), true); }
                }
                initProgress++;


            if (DEBUG) { writeToLine(lcdMain, "194 - Set Variables Initalized", true); }
            return true;
        }

        public void getPreferences(AIModule a)
        {
            initProgress = 0;
            string pref = Me.CustomData;
            int j;
            string[] prefs = pref.Split('\n');

            // Default Radius               
                try
                {
                    DefaultRadius = Int32.Parse(prefs[1].Split('|')[1]);
                }
                catch (Exception e)
                {
                    if (DEBUG) { Echo("204 - " + e.ToString()); }
                    exceptionHandler(e, 205);
                    DefaultRadius = 5000;
                }
                initProgress++;
            // Origin GPS                                                                                           //Origin = new GPSlocation("Origin",remote.GetPosition());             
                try
                {
                    string oGPS = prefs[2].Split('|')[1];
                    if (prefs[2].Length <= 12)
                    {
                        Origin = new GPSlocation("Origin", remote.GetPosition());
                        Origin.customInfo.Add("OriginType", "Stationary");
                        Origin.customInfo.Add("OriginComm", "none");
                        OriginComm = "none";
                        OriginType = "Stationary";
                    }
                    else
                    {
                        Origin = new GPSlocation(oGPS);
                        if (Origin.GPSeventLog.Length > 0) { eventLogger("Origin GPS Event Log", new string[]{ Origin.GPSeventLog } ); }
                        string comm = "";
                        string type = "";
                        if (Origin.customInfo.TryGetValue("OriginComm", out comm)) { OriginComm = comm; } else { OriginComm = "none"; }
                        if (Origin.customInfo.TryGetValue("OriginType", out type)) { OriginType = type; } else { OriginType = "Stationary"; }
                    }
                }
                catch (Exception e)
                {
                    if (DEBUG) { Echo("226 - " + e.ToString()); }
                    exceptionHandler(e, 227);
                    Origin = new GPSlocation("Origin", remote.GetPosition());
                    Origin.customInfo.Add("OriginType", "Stationary");
                    Origin.customInfo.Add("OriginComm", "none");
                    OriginComm = "none";
                    OriginType = "Stationary";
                }
                initProgress++;
            //Drone Status              
                try
                {
                    DroneStatus = prefs[3].Split('|')[1].Trim();
                    if (DroneStatus == "") { DroneStatus = "Idle"; }
                }
                catch (Exception e)
                {
                    if (DEBUG) { Echo("239 - " + e.ToString()); }
                    exceptionHandler(e, 240);
                    DroneStatus = "Idle";
                }
                initProgress++;
            // Runtime Count             
                try
                {
                    runCount = Int32.TryParse(prefs[4].Split('|')[1], out j) ? j : 0;
                    runCount++;
                }
                catch (Exception e)
                {
                    runCount = 0;
                    if (DEBUG) { Echo("249 - " + e.ToString()); }
                    exceptionHandler(e, 249);
                }
                initProgress++;
            // AIAttempts          
                try
                {
                    a.attempts = Int32.TryParse(prefs[5].Split('|')[1], out j) ? j : 0;
                }
                catch (Exception e)
                {
                    a.attempts = 0;
                    if (DEBUG) { Echo("257 - " + e.ToString()); }
                    exceptionHandler(e, 256);
                }
                initProgress++;
            // AISpacing          
                try
                {
                    a.coordSpacing = Int32.TryParse(prefs[6].Split('|')[1], out j) ? j : 0;
                }
                catch (Exception e)
                {
                    a.coordSpacing = 200;
                    if (DEBUG) { Echo("265 - " + e.ToString()); }
                    exceptionHandler(e, 263);
                }
                initProgress++;
            // AI Fitness         
                try
                {
                    string[] fitnessArray = prefs[7].Split('|')[1].Split('*');
                    for (int i = 0; i < fitnessArray.Length; i++)
                    {
                        a.aiFitness[i] = Int32.TryParse(fitnessArray[i], out j) ? j : 0;
                    }
                }
                catch (Exception e)
                {
                    if (DEBUG) { Echo("275- " + e.ToString()); }
                    exceptionHandler(e, 272);
                    a.aiFitness = new List<int>() { 0, -1, -2, -3, -5, -1 };
                }
                initProgress++;
        }

        public bool eventLogger(string eventName, string[] eventData, Exception e = null)
        {
            try
            {
                string output = eventName + "{ \n\t";
                foreach(string str in eventData)
                {
                    output += str + "\n\t";
                }

                if(e != null)
                {
                    output += e.Message + "\n \n\t" + e.StackTrace + "\n";
                }

                output += "}";

                eventLog.Add(output);
                if (DEBUG) { writeToLine(lcdMain, output, true); }
                if (LCD_DEBUG) { Echo(output); }
            } 
            catch (Exception ex) 
            {
                exceptionHandler(ex);
                return false;
            }
            return true;
        }

        public string exceptionHandler(Exception e, int codeLine = 0)
        {
            string exeptTXT = e.ToString().Split(':')[0].Split('.')[1];

            writeToLine(lcdMain, ("Error: " + exeptTXT), true);
            errorLog.Add(codeLine + " - Error: " + e.Message + "\nStack Trace ------->\n\t" + e.StackTrace + "\n");
            eventLogger("Error:", new string[] { e.Message, e.StackTrace, ("codeLine: " + codeLine) });

            // Dump Log
            foreach(string str in eventLog){lcdMain.CustomData += str + "\r\n";}

            return ("Error: " + exeptTXT);
        }
    #endregion
    #region LCD Methods              
        public void writeToLCD(IMyTextPanel lcd, string output, bool append)
        {
            // Applys text to LCD Screens                 
            ((IMyTextPanel)lcd).WritePublicText(output, append);
            ((IMyTextPanel)lcd).ShowPublicTextOnScreen();
        }

        public void writeToLine(IMyTextPanel lcd, string output, bool append)
        {
            string txtOut = output + "\r\n";
            writeToLCD(lcd, txtOut, append);
        }

        public string drawLCDStatus(IMyTextPanel lcd)
        {
            string[] header = { "Time", "Runtime Count", "Error Count", "AI Status" };
            DateTime now = DateTime.Now;
            string[] newOutput = { now.ToString("T"), runCount.ToString(), errorLog.Count.ToString(), DroneStatus };
            string[] oldOutput = lcd.CustomData.Split('\n');
            
            string output = evenTextSpace(header, lcd) + "\r\n"
                + matchTextSpace(header, newOutput, lcd) + "\r\n";

            lcd.CustomData = matchTextSpace(header, newOutput, lcd) + "\r\n";


            int counter = oldOutput.Length > 100 ? 100 : (oldOutput.Length);

            for (int i = 0; i < counter; i++)
            {
                if (LCD_DEBUG) { writeToLine(lcdMain, ("drawLCD(OldData) - i = " + i), true); }
                string str = oldOutput[i];
                if (str != "")
                {
                    output += str + "\r\n";
                    lcd.CustomData += str + "\r\n";
                }
            }
            return output;
        }

        public string drawLCDRemote(IMyTextPanel lcd)
        {

            return "Remote";
        }

        public string drawLCDFuel(IMyTextPanel lcd)
        {

            return "Fuel";
        }

        public string drawLCDDamage(IMyTextPanel lcd)
        {

            return "Damage";
        }

        public string drawLCDData(IMyTextPanel lcd)
        {
            string output = "";

            foreach (GPSlocation gps in knownCoords)
            {
                output = gps.ToString() + "\r\n";
            }

            return output;
        }

        public string drawErrorLog(IMyTextPanel lcd)
        {
            return "Error Log";
        }

        public string drawLCDInput(IMyTextPanel lcd) { return "LCD Input->Output"; }

        public string getLCDInput(IMyTextPanel lcd) { return "LCD Input"; }

        public void drawLCDMain(IMyTextPanel lcd)
        {
            writeToLCD(lcdMain, "", false);
            writeToLCD(lcdMain, "", true);
        }

        public string evenTextSpace(string[] text, IMyTextPanel lcd)
        {
            string spacer = " ";
            string output = "";

            float spacerCount = getSpacerCount(text, lcd);

            if (spacerCount > 8) { spacerCount = 8; }
            foreach (string str in text)
            {
                output += str;
                for (int i = 0; i < spacerCount; i++) { output += spacer; }
            }

            return output;
        }

        public string matchTextSpace(string[] strPrime, string[] strBeta, IMyTextPanel lcd)
        {
            string spacer = " ";
            string output = "";
            float spacerCount = getSpacerCount(strPrime, lcd);

            if (LCD_DEBUG)
            {
                writeToLine(lcdMain, ("matchTextSpace(" + strPrime.ToString() + "," + strBeta.ToString() + "," + lcd.CustomName + ")"), true);
                writeToLine(lcdMain, ("492 - LCD Panel (lcd) = " + lcd.CustomName), true);
                writeToLine(lcdMain, ("493 - strPrime = " + String.Join("|", strPrime)), true);
                writeToLine(lcdMain, ("494 - strBeta = " + String.Join("|", strBeta)), true);
                Echo("495 - strBeta Length = " + strBeta.Length);
            }

            for (int i = 0; i < strBeta.Length; i++)
            {
                output += strBeta[i];
                if (LCD_DEBUG) { writeToLine(lcdMain, ("501 - " + i + " - " + output), true); }
                float dataSpacer = 0.0f;
                try
                {
                    dataSpacer = (strPrime[i].Length + spacerCount) - strBeta[i].Length;
                    if (LCD_DEBUG)
                    {
                        writeToLine(lcdMain,"511 - strPrime[i].Length: " + strPrime[i].Length,true);
                        writeToLine(lcdMain, "512 - spacerCount: " + spacerCount,true);
                        writeToLine(lcdMain, "513 - strBeta[i].Length: " + strBeta[i].Length,true);
                        writeToLine(lcdMain, "514 - dataSpacer = " + dataSpacer,true);
                    }
                }
                catch (Exception e)
                {
                    if (LCD_DEBUG){
                        Echo("510 - " + e.ToString());
                        Echo("511 - strPrime[i].Length: " + strPrime[i].Length);
                        Echo("512 - spacerCount: " + spacerCount);
                        Echo("513 - strBeta[i].Length: " + strBeta[i].Length);
                        Echo("514 - dataSpacer = " + dataSpacer);
                    }
                    dataSpacer = 10.0f;
                }
                for (int j = 0; j < dataSpacer; j++)
                {
                    if (LCD_DEBUG) { writeToLine(lcdMain, ("520 - " + output), true); }
                    output += spacer;
                }
            }

            if (LCD_DEBUG) { writeToLine(lcdMain, ("525 - " + output), true); }
            return output;
        }

        public float getSpacerCount(string[] text, IMyTextPanel lcd)
        {
            float width = lcd.GetValue<float>("FontSize"); int j;
            int fontSize = lcdSettings.TryGetValue((double)width, out j) ? j : 25;


            float textCount = 0.0f;
            foreach (string str in text) { textCount += (float)str.Length; }
            return (fontSize - textCount) / (text.Length - 1);
        }
    #endregion
    #region Navigation Methods 
        public GPSlocation newCoordinate(AIModule a)
        {

            int selector = 0;

            string stamp = DateTime.Now.ToString("HHmmssfff");
            GPSlocation nGPS = null;
            bool valid = false;
            while (!valid)
            {
                for (int i = 0; i < 5; i++)
                {
                    selector = (a.aiFitness[selector] > a.aiFitness[i]) ? selector : i;
                    writeToLine(lcdMain, ("709 - i: " + i), true);
                    writeToLine(lcdMain, ("710 - selector = (a.aiFitness[" + selector + "] : " + a.aiFitness[selector] + ") > ( a.aiFitness[" + i + "] : " + a.aiFitness[i] + ")"), true);
                }

                writeToLine(lcdMain, ("712 - selector: " + selector), true);

                switch (selector)
                {
                    case 0:
                        nGPS = new GPSlocation(stamp, rndCoord());
                        nGPS.customInfo.Add("AISelector", ("" + selector));
                        break;
                    case 1:
                        nGPS = new GPSlocation(stamp, invCorrd());
                        nGPS.customInfo.Add("AISelector", ("" + selector));
                        break;
                    case 2:
                        if (knownCoords.Count >= 2)
                        {
                            GPSlocation addA = knownCoords[0]; 
                            GPSlocation addB = knownCoords[1];

                            knownCoords.RemoveAt(0);
                            knownCoords.RemoveAt(1);

                            Vector3D addC = addVectors(addA.gps, addB.gps);
                            nGPS = new GPSlocation(stamp, addC);
                            nGPS.customInfo.Add("AISelector", ("" + selector));

                            knownCoords.Add(addA);
                            knownCoords.Add(addB);
                        }
                        break;
                    case 3:
                        nGPS = new GPSlocation(stamp, dotVector());
                        nGPS.customInfo.Add("AISelector", ("" + selector));
                        break;
                    case 4:
                        nGPS = new GPSlocation(stamp, crossVector());
                        nGPS.customInfo.Add("AISelector", ("" + selector));
                        break;
                    case 5:
                        nGPS = new GPSlocation(stamp, poiCoord());
                        nGPS.customInfo.Add("AISelector", ("" + selector));
                        break;
                }

                   
                valid = true;
                try {
                    foreach (GPSlocation g in knownCoords)
                    {
                        if (!nGPS.checkNear(g.gps, a.coordSpacing))
                        {
                            valid = false;
                            a.aiFitness[selector]--;
                            break;
                        }
                    }
                }
                catch (Exception e) {
                    writeToLine(lcdMain,("755 - " + e.Message + "\n" +  e.StackTrace), true);
                    writeToLine(lcdMain, ("756 - selector:" + selector), true);
                    if (nGPS != null) { writeToLine(lcdMain, ("nGPS : " + nGPS.ToString()), true); } else { writeToLine(lcdMain, ("nGPS = null"), true); }
                    for (int i = 0; i < knownCoords.Count; i++) { writeToLine(lcdMain, ("KnownCoords[" + i + "] : " + knownCoords[i]), true); }
                }
                if (valid)
                {
                    writeToLine(lcdMain, nGPS.ToString(), true);
                    a.aiFitness[selector]++;
                    return nGPS;
                }
                    
            }
            return null;
        }

        public double genRandomNumber()
        {
            Random rnd = new Random();

            int direction = rnd.Next(0, 1);
            int number = rnd.Next(1, DefaultRadius);

            if (direction == 1)
            {
                return Convert.ToDouble(number);
            }
            else
            {
                return Convert.ToDouble(number * -1);
            }
        }
        
        public Vector3D rndCoord()
        {
            double x = genRandomNumber();
            x = x + Origin.gps.X;
            double y = genRandomNumber();
            y = y + Origin.gps.Y;
            double z = genRandomNumber();
            z = z = Origin.gps.Z;
            return new Vector3D(x,y,z);
        }

        public Vector3D invCorrd()
        {
            double x = (Current.gps.X - Origin.gps.X) * -1;
            x = x + Origin.gps.X;
            double y = (Current.gps.Y - Origin.gps.Y) * -1;
            y = y + Origin.gps.Y;
            double z = (Current.gps.Z - Origin.gps.Z) * -1;
            z = z + Origin.gps.Z;

            return new Vector3D(x, y, z); 
        }

        public Vector3D addVectors(Vector3D a, Vector3D b) 
        {
            double x = a.X + b.X;
            double y = a.Y + b.Y;
            double z = a.Z + b.Z;
            
            return new Vector3D(x, y, z); 
        }

        public Vector3D dotVector() { return new Vector3D(0, 0, 0); }

        public Vector3D crossVector() { return new Vector3D(0, 0, 0); }

        public Vector3D poiCoord() { return new Vector3D(0, 0, 0); }

    #endregion
    #region Nested Classes           
        public class GPSlocation
        {
            public string name;
            public Vector3D gps;
            public int fitness = 0;
            public int fitnessType = 0;
            public Dictionary<string, string> customInfo = new Dictionary<string, string>();

            public string GPSeventLog = "";

            public GPSlocation(string newName, Vector3D newGPS)
            {
                name = newName;
                gps = newGPS;
            }

            public GPSlocation(string storedGPS)
            {

                // "<Origin^{X:0 Y:0 Z:0}^0^OriginType:Stationary$OriginComm:none>"             

                char[] charsToTrim = { '<', '>', ' ' };
                string storeGPS = storedGPS.Trim();
                storeGPS = storeGPS.Trim(charsToTrim);
                string[] attr = storeGPS.Split('^');

                // Name              
                name = attr[0];

                // GPS              
                gps = recoverGPS(attr[1]);

                // Fitness              
                int fit; bool fitCheck = Int32.TryParse(attr[2], out fit);
                if (fitCheck) { fitness = fit; } else { fitness = 0; }

                // Custom Info             
                if (attr.Length == 4)
                {
                    string[] customAttr = attr[3].Split('$');
                    foreach (string str in customAttr)
                    {
                        str.Trim(' ');
                        if (str.Length > 3 || str != "")
                        {
                            //string strTest = str.Trim(new Char[]'>');              
                            string[] temp = str.Split(':');
                            try
                            {
                                customInfo.Add(temp[0], temp[1]);
                            }
                            catch (Exception e)
                            {
                                GPSeventLog += String.Format("Error Adding: {3}\r\n \tKey: {0}\r\n \tValue: {1}\r\n \r\n Stack Trace:\r\n{2}\r\n", temp[0], "value", e.ToString(), str);
                            }
                        }
                    }
                }
            }

            public MyWaypointInfo convertToWaypoint()
            {
                return new MyWaypointInfo(name, gps);
            }

            public Vector3D recoverGPS(string waypoint)
            {
                waypoint = waypoint.Trim(new Char[] { '{', '}' });
                string[] coord = waypoint.Split(' ');

                double x = double.Parse(coord[0].Split(':')[1]);
                double y = double.Parse(coord[1].Split(':')[1]);
                double z = double.Parse(coord[2].Split(':')[1]);

                return new Vector3D(x, y, z);
            }

            public bool checkNear(Vector3D gps2, int spacer)
            {
                double deltaX = (gps.X > gps2.X) ? gps.X - gps2.X : gps2.X - gps.X;
                double deltaY = (gps.Y > gps2.Y) ? gps.Y - gps2.Y : gps2.Y - gps.Y;
                double deltaZ = (gps.Z > gps2.Z) ? gps.Z - gps2.Z : gps2.Z - gps.Z;

                if (deltaX < spacer || deltaY < spacer || deltaZ < spacer) { return false; } else { return true; }
            }

            public void fitnessEval()
            {

            }

            public string[] getCustomInfo(string infoName)
            {
                string[] temp = { "" };
                return temp;
            }

            public void setCustomInfo(string infoName, string newValue, bool createNew) { }

            public override string ToString()
            {
                string custom = "";
                if (customInfo.Count != 0)
                {
                    foreach (KeyValuePair<string, string> item in customInfo)
                    {
                        custom += String.Format("{0}:{1}$", item.Key, item.Value);
                    }
                    custom = custom.TrimEnd('$');
                }
                else { custom = "0"; }

                string rtnString = String.Format("<{0}^{1}^{2}^{3}>", name, gps.ToString(), fitness, custom);
                return rtnString;
            }
        }

        public class AIModule
        {
            // [1: Random Coordinate - 2: Inverted Coordinate 3:Vector Addition - 4:Vector Dot Product - 5: Vector Cross Product - 6: Points of Interest]     
            public List<int> aiFitness = new List<int>() { 0, -1, -2, -3, -5, -1 };

            public int coordSpacing = 200;
            public int attempts = 0;

            public void checkResults()
            {
                int pRCounter = 0;
                for (int i = 0; i < aiFitness.Count; i++)
                {
                    if (aiFitness[i] <= -5)
                    {
                        pRCounter--;
                    }
                    else if (aiFitness[i] >= 5)
                    {
                        pRCounter++;
                    }

                }

                if (pRCounter == 6)
                {
                    aiFitness = new List<int>() { 0, -1, -2, -3, -5, -1 };
                    coordSpacing += 50;
                }
                else if (pRCounter == -6)
                {
                    aiFitness = new List<int>() { 0, -1, -2, -3, -5, -1 };
                    coordSpacing -= 50;
                }
            }
        }
    #endregion
    #region post-script
    }
}
#endregion
