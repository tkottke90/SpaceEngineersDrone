  
// Coniguration:  
int DefaultRadius = 5000;  
Vector3D originGPS;  
string OriginType;  
string OriginComm;  
string DroneStatus;
  
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
      
    getPreferences();    

//  Main Code //
 
    if(lcdMain != null){  
        
        GPSlocation g = new GPSlocation("test",remote.GetPosition());
        string store = g.ToString();
        writeToLCD(lcdMain,String.Format("Store = {0}\r\n", store),true);
                
        store = store.Trim(new Char[] {'<','>'});
        string[] temp = store.Split('|');

        writeToLCD(lcdMain,temp[1],true);

        GPSlocation h = new GPSlocation(store);
        h.name = "test2";
        writeToLCD(lcdMain,h.ToString(),true);
        
    }else{
        Echo("LCDMain = Null");
    }  

// Runtime End //

    
}  
  
public void getPreferences(){  
    string pref = Me.CustomData; 
    string[] prefs = pref.Split('\n'); 
 
    // Default Radius 
    DefaultRadius = Int32.Parse(prefs[1].Split('|')[1]); 
    // Origin Type 
    OriginType = prefs[2].Split('|')[1]; 
    // Origin Comm 
    OriginComm = prefs[3].Split('|')[1]; 
    // Origin GPS 
    string oGPStemp = prefs[4].Split('|')[1]; 
    if(oGPStemp != ""){ 
        originGPS = recoverGPS(oGPStemp); 
    }else{ 
        originGPS = remote.GetPosition(); 
    } 
    //Drone Status
    DroneStatus = prefs[5].Split('|')[1]; 
    if(DroneStatus.Length == 0){DroneStatus = "Idle";}
    
 
    string updatePrefs =  
        "* Preferences: * \r\n" + "Operating Radius|" + DefaultRadius + "\r\n" 
        + "OriginGPSType|" + OriginType + "\r\n" 
        + "OriginComm|" + OriginComm + "\r\n" 
        + "OriginGPS|" + originGPS.ToString() + "\r\n"
        + "DroneStatus|" + DroneStatus; 
    Me.CustomData = updatePrefs; 
}  
  
public Vector3D recoverGPS(string waypoint){     
    string[] coord = waypoint.Split(' ','}','{'); 
     
    double x = double.Parse(coord[1].Split(':')[1]); 
    double y = double.Parse(coord[2].Split(':')[1]); 
    double z = double.Parse(coord[3].Split(':')[1]); 
     
    return new Vector3D(x,y,z);  
}  
  
public void writeToLCD(IMyTextPanel lcd, string output, bool append){   
    	// Applys text to LCD Screens   
    	((IMyTextPanel)lcd).WritePublicText(output,append);   
	   ((IMyTextPanel)lcd).ShowPublicTextOnScreen();   
}  

public void writeLineToLCD(IMyTextPanel lcd, string output, bool append){    
    	// Applys text to LCD Screens    
    string out = output + "\r\n";
    	((IMyTextPanel)lcd).WritePublicText(out,append);    
	   ((IMyTextPanel)lcd).ShowPublicTextOnScreen();    
}  

public void drawLCDMain(IMyTextPanel lcd){
    writeToLCD(lcdMain,"",false);
    writeToLCD(lcdMain,"",true);
}
  
public MyWaypointInfo genNewCoord(){  
    Random rnd = new Random();  
  
    int x = genRandomNumber();  
    int y = genRandomNumber();  
    int z = genRandomNumber();  
      
    Vector3D coord = new Vector3D(x,y,z);  
  
    return new MyWaypointInfo("testCoord",coord);  
}  
  
public int genRandomNumber(){  
    Random rnd = new Random();  
      
    int direction = 1;  
    int number = rnd.Next(1,DefaultRadius);  
  
    if(direction == 1){  
        return number;  
    }else{  
        return (number * -1);  
    }  
}


public class GPSlocation { 
    public string name; 
    public Vector3D gps; 
    public int fitness = 0;    

    public GPSlocation (string newName, Vector3D newGPS){ 
        name = newName; 
        gps = newGPS;
    }

    public GPSlocation (string storedGPS){
        string storeGPS = storedGPS.Trim(new Char[] {'<','>'});
        string[] attr = storeGPS.Split('|');
        
        // Name
        name = attr[0].Split(':')[1];
        // GPS
        gps = recoverGPS(attr[1]);
        // Fitness
        int fit; bool fitCheck = Int32.TryParse(attr[2],out fit);
        if(fitCheck){fitness = fit;}else{fitness = 0;}
        
        
    }
    
    public MyWaypointInfo convertToWaypoint(){
        return new MyWaypointInfo(name,gps);
    }

    public Vector3D recoverGPS(string waypoint){      
        waypoint.Trim(new Char[] {'{','}'});
        string[] coord = waypoint.Split(' ');  
      
        double x = double.Parse(coord[0].Split(':')[1]);  
        double y = double.Parse(coord[1].Split(':')[1]);  
        double z = double.Parse(coord[2].Split(':')[1]);  
      
        return new Vector3D(x,y,z);   
    }  

    public void fitnessEval(){

    }

    public override string ToString(){
        string rtnString = String.Format("<name:{0}|{1}|{2}>",name,gps.ToString(),fitness);
        return rtnString;
    }
}


