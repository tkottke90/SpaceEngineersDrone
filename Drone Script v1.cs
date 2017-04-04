 
// Coniguration: 
int DefaultRadius = 5000; 
MyWaypointInfo originGPS; 
string OriginType; 
string OriginComm; 
 
// Variables  
    // Utility 
    IMyRemoteControl remote;  
    IMyTextPanel lcdMain; 
    List<IMyTextPanel> lcds = new List<IMyTextPanel>();  
    string terminalData;  
    string newTerminalData; 
 
    // Navigation 
    Vector3D patrolCoord; 
    Vector3D targetCoord; 
         
 
 
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
        if(lcdMain == null){newTerminalData +=  
                "Error: Missing LCDStatus - \r\n Please Add LCD Panel Named LCDStatus\r\n";} 
        writeToLCD(lcdMain,"",false); 
        // Other LCDs: 
     
    getPreferences();   

    writeToLCD(lcdMain, "Waypoints: \r\n", true); 
    if(lcdMain != null){ 
        List<MyWaypointInfo> waypoints = new List<MyWaypointInfo>();  
        remote.GetWaypointInfo(waypoints);  
		 
        MyWaypointInfo temp = genNewCoord();  
        waypoints.Add(temp);  
        remote.AddWaypoint(temp.Coords,temp.Name);         
    }else{Echo("LCDMain = Null");} 
 
} 
 
public void getPreferences(){ 
    string pref = Me.CustomData;
    string[] prefs = pref.Split('\n');

    for(int i = 1; i < prefs.Length; i++){
        writeToLCD(lcdMain,(i + ")" + prefs[i] + "\n"),true);
    }

    // Default Radius
    DefaultRadius = Int32.Parse(prefs[1].Split(':')[1]);
    // Origin Type
    OriginType = prefs[2].Split(':')[1];
    // Origin Comm
    OriginComm = prefs[3].Split(':')[1];
    // Origin GPS
    if(prefs[4].Split(':')[1] != ""){
        originGPS = recoverGPS(prefs[4].Split(':')[1]);
    }else{
        originGPS = remote.
    }
} 
 
public MyWaypointInfo recoverGPS(string waypoint){ 
    writeToLCD(lcdMain, waypoint, false);    


    return new MyWaypointInfo(); 
} 
 
public void writeToLCD(IMyTextPanel lcd, string output, bool append){  
    	// Applys text to LCD Screens  
    	((IMyTextPanel)lcd).WritePublicText(output,append);  
	   ((IMyTextPanel)lcd).ShowPublicTextOnScreen();  
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

public int fitnessTest(){}