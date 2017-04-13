   
// Coniguration:   
int DefaultRadius = 5000;     
string OriginType;   
string OriginComm;   
string DroneStatus; 
 
GPSlocation Origin; 
   
// Variables    
    // Utility   
    IMyRemoteControl remote;    
    IMyTextPanel lcdMain;   
    List<IMyTextPanel> lcds = new List<IMyTextPanel>();    
    string terminalData;    
    string newTerminalData;   
   
    // Navigation    
     
    // Performance 
 
   
   
public void Main(string argument){    
    	// Initialize Variables    
	   List<IMyTerminalBlock> list0 = new List<IMyTerminalBlock>();    
	   terminalData = Storage;      
	    
    	// Set Variables    
		    // Remote Control:    
	   	 GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list0);    
		    remote = (IMyRemoteControl) list0[0];    
		    // Main LCD:    
        lcdMain = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCDStatus");   
        if(lcdMain == null){newTerminalData +=  "Error: Missing LCDStatus - \r\n Please Add LCD Panel Named LCDStatus\r\n";}   
        writeToLCD(lcdMain, "", false); 
        // Other LCDs:     
    Origin = new GPSlocation("Origin",remote.GetPosition());
    getPreferences();     
 
//  Main Code // 
  
    if(lcdMain != null){   
         
        GPSlocation g = new GPSlocation("test",remote.GetPosition()); 
        string store = g.ToString(); 
 
         
    }else{ 
        Echo("LCDMain = Null"); 
    }   
 
// Runtime End // 
 
     
}   
   
public void getPreferences(){   
    string pref = Me.CustomData;  
    string[] prefs = pref.Split('\n'); 
    
    for(int i = 1; i < prefs.Length; i++){
        writeToLine(lcdMain,(String.Format("{0}a) {1}", i, prefs[i].Split('|')[0])),true);
        writeToLine(lcdMain,(String.Format("{0}b) {1}", i, prefs[i].Split('|')[1])),true);
    }

    writeToLine(lcdMain,"",true);


    // Default Radius  
    DefaultRadius = Int32.Parse(prefs[1].Split('|')[1]);  
    // Origin GPS  
                                                                                                        //Origin = new GPSlocation("Origin",remote.GetPosition());
    string oGPS = prefs[2].Split('|')[1];
    if(prefs[2].Length <= 12){  
        Origin = new GPSlocation("Origin",remote.GetPosition());
        Origin.customInfo.Add("OriginType","Stationary");  
        Origin.customInfo.Add("OriginComm", "none");
        OriginComm = "none";
        OriginType = "Stationary"; 
    }else{  
        Origin = new GPSlocation(oGPS);
        writeToLine(lcdMain,Origin.eventLog,true);
        string comm = "";
        string type = "";
        if(Origin.customInfo.TryGetValue("OriginComm", out comm)){OriginComm = comm;}else{OriginComm = "none";}
        if(Origin.customInfo.TryGetValue("OriginType", out type)){OriginType = type;}else{OriginType = "Stationary";}  
    }
    //Drone Status 
    DroneStatus = prefs[3].Split('|')[1].Trim();  
    if(DroneStatus == ""){DroneStatus = "Idle";} 
     
  
    string updatePrefs =   
        "* Preferences: * \r\n" 
        + "Operating Radius|" + DefaultRadius + "\r\n"  
        + "OriginGPS|" + Origin.ToString() + "\r\n" 
        + "DroneStatus|" + DroneStatus; 
    Me.CustomData = updatePrefs;  
} 
   
public void writeToLCD(IMyTextPanel lcd, string output, bool append){    
    	// Applys text to LCD Screens    
    	((IMyTextPanel)lcd).WritePublicText(output,append);    
	   ((IMyTextPanel)lcd).ShowPublicTextOnScreen();    
}   
 
public void writeToLine(IMyTextPanel lcd, string output, bool append){ 
    string txtOut = output + "\r\n"; 
    writeToLCD(lcd,txtOut,append); 
} 
 
public void drawLCDMain(IMyTextPanel lcd){ 
    writeToLCD(lcdMain,"",false); 
    writeToLCD(lcdMain,"",true); 
} 
   
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
        /*
        List:
            1) Name
            2) {X:0 Y:0 Z:0}
            3) 0
            4) OriginType:Stationary$OriginComm:none
        
         */

        /*

        1) char[] charsToTrim = char[] charsToTrim = {'<','>'};

        2) string storeGPS = storedGPS.Trim();
           storeGPS = storeGPS.Trim(charsToTrim); 

         */


        char[] charsToTrim = {'<','>'};
        string storeGPS = storedGPS.Trim(charsToTrim); 
        //storeGPS = storeGPS.TrimEnd('>');
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
                eventLog += String.Format("str.Length: {0}\r\n",str.Length);
                if(str.Length > 3 || str != ""){
                    //string strTest = str.Trim(new Char[]'>'); 
                    string[] temp = str.Split(':'); 
                    try{  
                        customInfo.Add(temp[0],temp[1]); 
                        eventLog += String.Format("Added: {0} \r\n \tKey: {1}\r\n \tValue: {2}\r\n",str,temp[0],temp[1]); 
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

