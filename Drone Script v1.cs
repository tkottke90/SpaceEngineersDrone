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
    string terminalData;              
    string newTerminalData;       
     
    public Dictionary<double,int> lcdSettings = new Dictionary<double,int>(){      
        {0.5,52},{0.75,34},{1,25},{2,12}      
    };            
      
    // Block References       
    IMyRemoteControl remote;              
    IMyTextPanel lcdMain;         
    List<IMyGyro> blackbox = new List<IMyGyro>();         
    IMyLaserAntenna comm;         
    List<IMyTextPanel> lcds = new List<IMyTextPanel>();                 
    List<string> errorLog = new List<string>();         
             
    // Navigation              
    List<GPSlocation> knownCoords = new List<GPSlocation>();       
    List<GPSlocation> poi = new List<GPSlocation>();  // Script will keep coordinates        
        
    // AI Variables       
    int coordSpacing = 200;            
    int[] genCoordFitness = new int[6]; // [1: Random Coordinate - 2: Inverted Coordinate 3:Vector Addition - 4:Vector Dot Product - 5: Vector Cross Product - 6: Points of Interest]           
    int attempts = 0;         
             
public void Main(string argument){              
  
    AIModule AI = new AIModule();  
  
    getPreferences(AI);             
       
       
    //Storage = "<Massive unknown object^{X:42613 Y:147788 Z:-129714}^0^0>\r\n<Unknown object^{X:35399 Y:142334 Z:-134776}^0^0>\r\n<Massive Asteroid^{X:39759 Y:151628 Z:-130483}^0^0>";       
           
//  Main Code //           
            
    if(setVariables(AI)){                   
       
       
    // Get Status and Respond        
    switch(DroneStatus){         
        case "Idle":         
            // Check for Comm Connection -> Send Data if available         
                // Set Status -> Tranasmitting Data        
            // Create new waypoint -> Check records for waypoint -> if new waypoint, investigate        
                int selector = 0; for(int i = 1; i < 6; i++){  
                    if(AI.aiFitness[i] > AI.aiFitness[selector]){selector = i;}  
                }  
                MyWaypointInfo newWaypoint = newCoordinate(selector);  

            break;        
        case "Exploring":        
            // Autopilot enabled and going to destination        
                    
            break;       
        default:        
            DroneStatus = "Idle";        
            break;       
    }  
    string time = "Current Time:" + DateTime.Now.ToString();          
    string[] header =  {"Runtime Count", "Error Count", "AI Status"};     
    string[] newOutput = {runCount.ToString(), errorLog.Count.ToString(), DroneStatus};    
    writeToLine(lcdMain,evenTextSpace(header,lcdMain),true);     
    writeToLine(lcdMain,matchTextSpace(header, newOutput,lcdMain),true);    
                   
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
        + "RuntimeCount|" + runCount + "\r\n"      
        + "AIAttempts|" + attempts + "\r\n"       
        + "AICoordinateSpacing|" + coordSpacing + "\r\n"       
        + "AICalcFitness|"        
        + String.Format("{0}-{1}-{2}-{3}-{4}-{5}", genCoordFitness[0],genCoordFitness[1],genCoordFitness[2],genCoordFitness[3],genCoordFitness[4],genCoordFitness[5]);           
         
    Me.CustomData = updatePrefs;          
}             
         
// Utility Methods         
    public bool setVariables(AIModule a){         
    // Initialize Variables              
        List<IMyTerminalBlock> l0 = new List<IMyTerminalBlock>(); // Remote Control         
        List<IMyTerminalBlock> l1 = new List<IMyTerminalBlock>(); // LCD Pannels             
        List<IMyTerminalBlock> l2 = new List<IMyTerminalBlock>(); // Laser Antenna         
        List<IMyTerminalBlock> l3 = new List<IMyTerminalBlock>(); // Gyroscopes         
        List<IMyTerminalBlock> l4 = new List<IMyTerminalBlock>(); // Sensors       
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
                //txt.SetValue("FontSize", (float)0.5);         
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
                GPSlocation gps = new GPSlocation(str); bool compare = false;       
                       
                foreach(GPSlocation g in knownCoords){       
                    if(gps.ToString() == g.ToString()){compare = true;}                            
                }       
                       
                if(!compare){knownCoords.Add(gps);writeToLine(lcdMain,("Added: " + str),true);}       
            }        
        
        return true;         
    }         
         
    public void getPreferences(AIModule a){             
        string pref = Me.CustomData;            
        int j;      
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
                runCount = Int32.TryParse(prefs[4].Split('|')[1], out j) ? j : 0;           
                runCount++;         
            }catch(Exception e){         
                runCount = 0;         
                exceptionHandler(e);          
            }       
         // AIAttempts       
            try{       
                a.attempts = Int32.TryParse(prefs[5].Split('|')[1], out j) ? j : 0;       
            }catch(Exception e){       
                a.attempts = 0;       
                exceptionHandler(e);       
            }       
        // AISpacing       
            try{       
                a.coordSpacing = Int32.TryParse(prefs[6].Split('|')[1], out j) ? j : 0;       
            }catch(Exception e){       
                a.coordSpacing = 200;       
                exceptionHandler(e);       
            }       
        // AI Fitness      
            try{      
                string[] fitnessArray = prefs[7].Split('|')[1].Split('-');      
                for(int i = 0; i < fitnessArray.Length; i++){      
                    a.aiFitness[i] = Int32.TryParse(fitnessArray[i], out j) ? j : 0;      
                }      
            }catch(Exception e){      
                exceptionHandler(e);      
                a.aiFitness = new Dictionary<int,int>(){ 
                    {1,0},{2,-1},{3,-2},{4,-3},{5,-5},{6,-1} 
                };      
            }      
    }           
         
    public string exceptionHandler(Exception e){          
        string exeptTXT = e.ToString().Split(':')[0].Split('.')[1];          
             
        writeToLine(lcdMain,("Error: " + exeptTXT),true);           
        errorLog.Add("Error: " + e.Message + "\nStack Trace ------->\n\t" + e.StackTrace + "\n");         
        return "0";              
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
        string[] header =  {"Time", "Runtime Count", "Error Count", "AI Status"};       
        DateTime now = DateTime.Now;  
        string[] newOutput = {now.ToString("T"), runCount.ToString(), errorLog.Count.ToString(), DroneStatus};       
        string[] oldOutput = lcd.CustomData.Split('\n');       
      
        string output = evenTextSpace(header,lcd) + "\r\n"   
            + matchTextSpace(header,newOutput,lcd) + "\r\n";    
            
        lcd.CustomData = matchTextSpace(header,newOutput,lcd) + "\r\n";   
    
        for(int i = 0; i < 100; i++){    
            string str = oldOutput[i];  
            if(str != ""){  
                output += str + "\r\n";    
                lcd.CustomData += str + "\r\n";    
            }  
        }    
        return output;      
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
         
    public string evenTextSpace(string[] text, IMyTextPanel lcd){     
        string spacer = " ";      
        string output = "";     
    
        float spacerCount = getSpacerCount(text,lcd);    
    
        if(spacerCount > 8){spacerCount = 8;}    
        foreach(string str in text){     
            output += str;     
            for(int i = 0; i < spacerCount;i++){output += spacer;}     
        }       
     
        return output;     
    }     
    
    public string matchTextSpace(string[] strPrime, string[] strBeta, IMyTextPanel lcd){    
        string spacer = " ";    
        string output = "";    
        float spacerCount = getSpacerCount(strPrime,lcd);    
    
        for(int i = 0; i < strBeta.Length; i++){    
            output += strBeta[i];    
               
            float dataSpacer = (strPrime[i].Length + spacerCount) - strBeta[i].Length;    
            for(int j = 0; j < dataSpacer; j++){output += spacer;}    
        }    
   
        return output;   
    }    
    
    public float getSpacerCount(string[] text, IMyTextPanel lcd){    
        float width = lcd.GetValue<float>("FontSize"); int j;      
        int fontSize = lcdSettings.TryGetValue((double)width, out j) ? j : 25;      
             
     
        float textCount = 0.0f;      
        foreach(string str in text){textCount += (float)str.Length;}    
        return (fontSize - textCount)/(text.Length - 1);    
    }    
    
// Navigation Methods         
    public MyWaypointInfo genNewCoord(){             
        /*  
            1: Random Coordinate  
            2: Inverted Coordinate   
            3: Vector Addition   
            4: Vector Dot Product  
            5: Vector Cross Product  
        */  
        int selector = 0;  
        //for(int i = 0; i < 5, i++){}  
  
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
      
    public GPSlocation newCoordinate(int selector){
        int stamp = DateTime.Now.Hour + 
        
        switch(selector){
            case 1:
                return new GPSlocation()
                break;
        }
    }  
  
    public Vector3D rndCoord(){return new Vector3D(0,0,0);} 
  
    public Vector3D invCorrd(){return new Vector3D(0,0,0);} 
  
    public Vector3D addVectors(){return new Vector3D(0,0,0);} 
  
    public Vector3D dotVector(){return new Vector3D(0,0,0);}  
  
    public Vector3D poiCoord(){return new Vector3D(0,0,0);}  
  
// Nested Classes         
    public class GPSlocation {            
        public string name;            
        public Vector3D gps;            
        public int fitness = 0;  
        public int fitnessType = 0;      
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
             
         public bool checkNear(Vector3D gps2){        
            double deltaX = (gps.X > gps2.X) ? gps.X - gps2.X : gps2.X - gps.X;        
            double deltaY = (gps.Y > gps2.Y) ? gps.Y - gps2.Y : gps2.Y - gps.Y;        
            double deltaZ = (gps.Z > gps2.Z) ? gps.Z - gps2.Z : gps2.Z - gps.Z;        
       
            if(deltaX < 200 || deltaY < 200 || deltaZ < 200){return false;}else{return true;}       
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
  
    public class AIModule {  
        // [1: Random Coordinate - 2: Inverted Coordinate 3:Vector Addition - 4:Vector Dot Product - 5: Vector Cross Product - 6: Points of Interest]  
        public Dictionary<int,int> aiFitness = new Dictionary<int,int>(){  
            {1,0},{2,-1},{3,-2},{4,-3},{5,-5},{6,-1}  
        };  
          
        public int coordSpacing = 200;     
        public int attempts = 0;   
    }