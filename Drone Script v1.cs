/*
	Instructions:
		1) Add LCD Panel and call it LCDStatus




*/

// Variables
IMyRemoteControl remote;
IMyTextPanel lcdMain;
IMyTextPanel lcdPanels;

boolean compSuccess = true;
integer sessionCount = 0;
string terminalData;
string newTerminalData;

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
			newTerminalData += "Error: Missing LCDStatus - \r\n Please Add LCD Panel Named LCDStatus\r\n";
			Echo("Error Occured - See Custom Data");
			compSuccess = false
		}
		// Other LCDs
		GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(list0);
		for(int i = 0; i < list0.count;i++){lcdPanels.add((IMyTextPanel)list0[i]);}
		
	// Extract terminalData


	// End of Run
	writeToLCD()
}


public void setAutoPilot(Vector3D coord){
	
	
}

public void writeToLCD(IMyTextPanel lcd, string output, bool append){
	// Applys text to LCD Screens
	((IMyTextPanel)lcd).WritePublicText(output,append);
	((IMyTextPanel)lcd).ShowPublicTextOnScreen();
}