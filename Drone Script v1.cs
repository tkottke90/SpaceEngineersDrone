// Coniguration:      
int DefaultRadius = 5000;        
string OriginType;      
string OriginComm;      
string DroneStatus;    
    
GPSlocation Origin;    
GPSlocation Current;  
      
// Variables       
    // Utility      
    int runCount;  
    IMyRemoteControl remote;       
    IMyTextPanel lcdMain;  
    List<IMyGyro> blackbox = new List<IMyGyro>();  
    IMyLaserAntenna comm;  
    List<IMyTextPanel> lcds = new List<IMyTextPanel>();       
    string terminalData;       
    string newTerminalData;      
    List<string> errorLog = new List<string>();  
      
    // Navigation       
    List<GPSlocation> knownCoords = new List<GPSlocation>(); 
 
    // Performance    
    
      
      
public void Main(string argument){       
         
    getPreferences();      


    //Storage = "<Massive unknown object^{X:42613 Y:147788 Z:-129714}^0^0>\r\n<Unknown object^{X:35399 Y:142334 Z:-134776}^0^0>\r\n<Massive Asteroid^{X:39759 Y:151628 Z:-130483}^0^0>";
    
//  Main Code //    
     
    if(setVariables()){      
         foreach(GPSlocation gps in knownCoords){writeToLine(lcdMain,("KnownCoord: " + gps.ToString()),true);}

        // Get Status and Respond 
        switch(DroneStatus){ 
            case "Idle": 
                // Check for Comm Connection -> Send Data if available 
                //  
                writeToLine(lcdMain,"Idle",true);
                break;
        } 
 
            
    }else{    
        Echo("Error Initilizing");    
    }      
    
// Runtime End //    
    
      foreach(string str in errorLog){Echo(str);}  
  
    string updatePrefs =      
        "* Preferences: * \r\n"    
        + "Operating Radius|" + DefaultRadius + "\r\n"     
        + "OriginGPS|" + Origin.ToString() + "\r\n"    
        + "DroneStatus|" + DroneStatus + "\r\n"  
        + "RuntimeCount|" + runCount;    
    Me.CustomData = updatePrefs;   
}      
  
// Utility Methods  
    public bool setVariables(){  
    // Initialize Variables       
        List<IMyTerminalBlock> l0 = new List<IMyTerminalBlock>(); // Remote Control  
        List<IMyTerminalBlock> l1 = new List<IMyTerminalBlock>(); // LCD Pannels      
        List<IMyTerminalBlock> l2 = new List<IMyTerminalBlock>(); // Laser Antenna  
        List<IMyTerminalBlock> l3 = new List<IMyTerminalBlock>(); // Gyroscopes  
        terminalData = Storage;         
          
    // Set Variables       
        // Remote Control:       
            GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(l0);  
            remote = (IMyRemoteControl)l0[0]; 
            if(remote == null){errorLog.Add("Error: Missing Remote Control - \r\n Please Add a Remote Control To Ship\r\n");return false;}   
        // Main LCD:       
            lcdMain = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCDMain");      
            if(lcdMain == null){errorLog.Add("Error: Missing LCDStatus - \r\n Please Add LCD Panel Named LCDMain to Ship\r\n");return false;}      
            writeToLCD(lcdMain, "", false);    
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
            foreach(IMyTextPanel txt in l1){   
                string output = "";  
                writeToLCD(txt,"",false);  
                txt.SetValue("FontSize", (float)0.5);  
                string[] customData = txt.CustomData.Split('\n');  
  
                if(txt.CustomName.Contains("[lcdStatus]")){output += drawLCDStatus(txt);}  
                if(txt.CustomName.Contains("[lcdRemote]")){output += drawLCDRemote(txt);}  
                if(txt.CustomName.Contains("[lcdFuel]")){output += drawLCDFuel(txt);}  
                if(txt.CustomName.Contains("[lcdDamage]")){output += drawLCDDamage(txt);}  
                if(txt.CustomName.Contains("[lcdData]")){output += drawLCDData(txt);}  
  
                try{  
                    writeToLCD(txt,output,true);  
                }catch(Exception e){  
                    writeToLine(lcdMain,exceptionHandler(e),true);  
                }  
            }  
  
        // BlackBox Storage in Gyroscopes  
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(l3);   
  
            if(l3.Count != 0){  
                for(int i = 0; i < l3.Count;i++){  
                    string storageData = l3[i].CustomData;  
                    string[] sData = storageData.Split('\n')[0].Split(':');  
  
                    if(!blackbox.Contains(l3[i])){  
                        blackbox.Add((IMyGyro)l3[i]); 
                        l3[i].CustomData = ""; 
                    }  
                }   
            }else{return false;} 
  
        // Laser Antennas  
            //GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(l2); 
            //comm = (IMyLaserAntenna)l2[0];  
         
        // Get Known Data 
            string[] rawData = terminalData.Split('\n'); 
            foreach(string str in rawData){ 
                GPSlocation gps = new GPSlocation(str);
                if(!knownCoords.Contains(gps)){
                    writeToLine(lcdMain,str,true);
                    knownCoords.Add(gps); 
                }
            } 
 
        return true;  
    }  
  
    public void getPreferences(){      
        string pref = Me.CustomData;     
        string[] prefs = pref.Split('\n');     
      
        // Default Radius     
        DefaultRadius = Int32.Parse(prefs[1].Split('|')[1]);     
        // Origin GPS                                                                                           //Origin = new GPSlocation("Origin",remote.GetPosition());   
        string oGPS = prefs[2].Split('|')[1];   
        if(prefs[2].Length <= 12){     
            Origin = new GPSlocation("Origin",remote.GetPosition());   
            Origin.customInfo.Add("OriginType","Stationary");     
            Origin.customInfo.Add("OriginComm", "none");   
            OriginComm = "none";   
            OriginType = "Stationary";    
        }else{     
            Origin = new GPSlocation(oGPS);   
            if(Origin.eventLog.Length > 0){writeToLine(lcdMain,Origin.eventLog,true);}   
            string comm = "";   
            string type = "";   
            if(Origin.customInfo.TryGetValue("OriginComm", out comm)){OriginComm = comm;}else{OriginComm = "none";}   
            if(Origin.customInfo.TryGetValue("OriginType", out type)){OriginType = type;}else{OriginType = "Stationary";}     
        }   
        //Drone Status    
        DroneStatus = prefs[3].Split('|')[1].Trim();     
        if(DroneStatus == ""){DroneStatus = "Idle";} 
        // Runtime Count   
        try{  
            int j;   
            runCount = Int32.TryParse(prefs[4].Split('|')[1], out j) ? j : 0;    
            runCount++;  
        }catch(Exception e){  
            runCount = 0;  
            exceptionHandler(e);   
        }    
    }    
  
    public string exceptionHandler(Exception e){   
        string exeptTXT = e.ToString().Split(':')[0].Split('.')[1];   
      
        writeToLine(lcdMain,("Error: " + exeptTXT),true);    
          
        switch(exeptTXT){   
            case "IndexOutOfRangeException":   
                errorLog.Add("Error: " + e.Message + "\nStack Trace ------->\n\t" + e.StackTrace + "\n");  
                return "0";   
            default:   
                return"0";   
        }  
    }  
  
// LCD Methods    
    public void writeToLCD(IMyTextPanel lcd, string output, bool append){       
        // Applys text to LCD Screens       
        ((IMyTextPanel)lcd).WritePublicText(output,append);       
        ((IMyTextPanel)lcd).ShowPublicTextOnScreen();       
    }      
      
    public void writeToLine(IMyTextPanel lcd, string output, bool append){    
        string txtOut = output + "\r\n";    
        writeToLCD(lcd,txtOut,append);    
    }    
  
    public string drawLCDStatus(IMyTextPanel lcd){  
  
        return "Status";  
    }  
  
    public string drawLCDRemote(IMyTextPanel lcd){  
  
        return "Remote";  
    }  
  
    public string drawLCDFuel(IMyTextPanel lcd){  
  
        return "Fuel";  
    }  
  
    public string drawLCDDamage(IMyTextPanel lcd){  
  
        return "Damage";  
    }  
  
    public string drawLCDData(IMyTextPanel lcd){  
        string output = ""; 
 
        foreach(GPSlocation gps in knownCoords){ 
            output = gps.ToString() + "\r\n"; 
        } 
 
        return output;  
    }  
  
    public string drawErrorLog(IMyTextPanel lcd){  
        return "Error Log";  
    }  
  
    public string drawLCDInput(IMyTextPanel lcd){return "LCD Input->Output";}  
  
    public string getLCDInput(IMyTextPanel lcd){return "LCD Input";}  
  
    public void drawLCDMain(IMyTextPanel lcd){    
        writeToLCD(lcdMain,"",false);    
        writeToLCD(lcdMain,"",true);    
    }    
  
// Navigation Methods  
    public MyWaypointInfo genNewCoord(){      
        Random rnd = new Random();      
          
        double x = genRandomNumber();    
        x = x + Origin.gps.X;      
        double y = genRandomNumber();    
        y = y + Origin.gps.Y;      
        double z = genRandomNumber();    
        z = z = Origin.gps.Z;      
              
        Vector3D coord = new Vector3D(x,y,z);      
          
        return new MyWaypointInfo("testCoord",coord);      
    }      
          
    public double genRandomNumber(){      
        Random rnd = new Random();      
              
        int direction = rnd.Next(0,1);      
        int number = rnd.Next(1,DefaultRadius);      
          
        if(direction == 1){      
            return Convert.ToDouble(number);      
        }else{      
            return Convert.ToDouble(number * -1);      
        }      
    }  
  
// Nested Classes  
    public class GPSlocation {     
        public string name;     
        public Vector3D gps;     
        public int fitness = 0;    
        public Dictionary<string,string> customInfo = new Dictionary<string,string>();    
      
        public string eventLog = "";   
      
        public GPSlocation (string newName, Vector3D newGPS){     
            name = newName;     
            gps = newGPS;    
        }    
      
        public GPSlocation (string storedGPS){    
      
            // "<Origin^{X:0 Y:0 Z:0}^0^OriginType:Stationary$OriginComm:none>"   
      
            char[] charsToTrim = {'<','>',' '};   
            string storeGPS = storedGPS.Trim();   
            storeGPS = storeGPS.Trim(charsToTrim);    
            string[] attr = storeGPS.Split('^');    
              
            // Name    
            name = attr[0];    
              
            // GPS    
            gps = recoverGPS(attr[1]);    
              
            // Fitness    
            int fit; bool fitCheck = Int32.TryParse(attr[2],out fit);    
            if(fitCheck){fitness = fit;}else{fitness = 0;}    
              
            // Custom Info   
            if(attr.Length == 4){   
            string[] customAttr = attr[3].Split('$');   
            foreach(string str in customAttr){   
                    str.Trim(' ');    
                    if(str.Length > 3 || str != ""){   
                        //string strTest = str.Trim(new Char[]'>');    
                        string[] temp = str.Split(':');    
                        try{     
                            customInfo.Add(temp[0],temp[1]);     
                        }catch(Exception e){     
                            eventLog += String.Format("Error Adding: {3}\r\n \tKey: {0}\r\n \tValue: {1}\r\n \r\n Stack Trace:\r\n{2}\r\n",temp[0],"value",e.ToString(),str);   
                        }    
                    }   
                }   
            }   
        }    
          
        public MyWaypointInfo convertToWaypoint(){    
            return new MyWaypointInfo(name,gps);    
        }    
      
        public Vector3D recoverGPS(string waypoint){          
            waypoint = waypoint.Trim(new Char[] {'{','}'});    
            string[] coord = waypoint.Split(' ');      
              
            double x = double.Parse(coord[0].Split(':')[1]);      
            double y = double.Parse(coord[1].Split(':')[1]);      
            double z = double.Parse(coord[2].Split(':')[1]);      
              
            return new Vector3D(x,y,z);       
        }      
      
        public void fitnessEval(){    
      
        }    
      
        public override string ToString(){    
            string custom = "";    
            if(customInfo.Count != 0){       
                foreach (KeyValuePair<string,string> item in customInfo)    
                {    
                    custom += String.Format("{0}:{1}$",item.Key,item.Value);    
                }    
                custom = custom.TrimEnd('$');   
            }else{custom = "0";}   
      
            string rtnString = String.Format("<{0}^{1}^{2}^{3}>",name,gps.ToString(),fitness,custom);    
            return rtnString;    
        }    
    }