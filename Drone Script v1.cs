/*
	Instructions:
		1) Add LCD Panel and call it LCDStatus




*/

// Configuration:										

const DefaultRadius = 5000;

// Variables
IMyRemoteControl remote;
IMyTextPanel lcdMain;
IMyTextPanel lcdPanels;

boolean compSuccess = true;
integer sessionCount = 0;
string terminalData;
string log;

string status = "";

public void Main(string argument){
	// Initialize Variables
	List<IMyTerminalBlock> list0 = new List<IMyTerminalBlock>();
	terminalData = Me.CustomData;
	
	// Set Variables
		// Remote Control:
		GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list0);
		remote = (IMyRemoteControl) list0[0];
		// Main LCD:
		lcdMain = (IMyTextPanel) GridTerminalSystem.GetBlocksWithName("LCDStatus");
		if(lcdMain == null){
			addErrorMessage("Error: Missing LCDStatus - \r\n Please Add LCD Panel Named LCDStatus\r\n")
			compSuccess = false
		}
		// Other LCDs
		GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(list0);
		for(int i = 0; i < list0.count;i++){lcdPanels.add((IMyTextPanel)list0[i]);}
		
	// Extract terminalData

	List<MyWaypointInfo> waypoints = new List<MyWaypointInfo>(); 
	remote.GetWaypointInfo(waypoints); 
	for(int i = 0; i < waypoints.Count; i++){ 
		string name = waypoints[i].Name; 
		Vector3D coords = waypoints[i].Coords; 
		writeToLCD(lcdMain,(i + ")" + name + ":\r\n"),true); 
		writeToLCD(lcdMain,(coords.ToString() + "\r\n"),true); 
	}


	// End of Run
		// Write Any Error Messages
		if(log.length > 0){writeToLCD(lcdMain,log,true);}
		// Update Drone Data
}

/*
	Terminal Data:
	- Origin
	- Sesson Count 
	- 


 */

public MyWaypointInfo genNewCoord(){
    Random rnd = new Random();

    int x = rnd.Next(1,DefaultRadius);
    int y = rnd.Next(1,DefaultRadius);
    int z = rnd.Next(1,DefaultRadius);
    
    Vector3D coord = new Vector3D(x,y,z);

    return new MyWaypointInfo("testCoord",coord);
}

public void addErrorMessage(string error){
	log += error + "\r\n";
	Echo("Error Occured - See Custom Data");
}

public void setAutoPilot(Vector3D coord){
	
	
}

public void writeToLCD(IMyTextPanel lcd, string output, bool append){
	// Applys text to LCD Screens
	((IMyTextPanel)lcd).WritePublicText(output,append);
	((IMyTextPanel)lcd).ShowPublicTextOnScreen();
}