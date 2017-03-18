// Variables
IMyRemoteControl remote;
IMyTextPanel[] lcdMain;

string terminalData;


public void Main(string argument){
	// Initialize Variables
	List<IMyTerminalBlock> list0 = new List<IMyTerminalBlock>();
	terminalData = Me.CustomData;
	
	// Set Variables
		// Remote Control:
		GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(list0);
		remote = (IMyRemoteControl) list0[0];
		// Main LCD:
		GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(list0);
		for(int i = 0; i < list0.count;i++){lcdMain.add((IMyTextPanel)list0[i]);}
		
}


public void setAutoPilot(Vector3D coord){
	
	
	
}

public void writeToLCD(IMyTextPanel lcd, string output, bool append){
	// Applys text to LCD Screens
	((IMyTextPanel)lcd).WritePublicText(output,append);
	((IMyTextPanel)lcd).ShowPublicTextOnScreen();
}