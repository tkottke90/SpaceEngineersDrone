/* v:0.33 (1.144 compatible) 
In-game script by MMaster 
 
http://steamcommunity.com/sharedfiles/filedetails/?id=447360835&insideModal=0&requirelogin=1 
 
Records GPS Waypoints to LCD or Text Panel to make it easy to make Autopilot Waypoints. 
Allows automatic recording as you move as well as manual. 
 
Open control panel of LCD or Text Panel and click Edit Public Text to add the waypoints to your GPS. 
You just need to 'Show on HUD' in GPS screen if you want to see them. 
 
 * Gets position from first found of the following (in order): remote control, cockpit 
 * You can override the source block by adding "SOURCE:Name Of Block" to programmable block name. 
 * Supports 3 actions entered to Programmable Block Run Argument (without quotes):  
 * "add PREFIX" - adds current GPS coordinates with PREFIX in name of waypoint 
 * "undo" - removes last added waypoint 
 * "reset" - clears the LCD / text panel 
 
MAKE SURE THAT YOU OWN ALL THE BLOCKS! 
 
Read Change Notes (above screenshots) for latest updates and new features. 
I notify about updates in steam group & twitter so follow if interested.  
 
If you like this script, please give it positive rating, if you don't like it then please 
let me know why so I can improve it & learn what I did wrong. 
Please DO NOT publish this script or its derivations without my permission! Feel free to use it in blueprints! 
 
QUICK GUIDE 
1. Load this script to programmable block 
2. Make LCD or Text panel and add RECORD to the name of it 
 * eg: Text Panel 1 RECORD 
3. Run the programmable block with argument "add Test" (without quotes) 
 * this will add waypoint with name "Test Waypoint 001" to the LCD 
4. Each time you run the script with "add Test" argument new waypoint will be added 
 * new waypoint is always on top so you can see as waypoints are added 
5. You can run it with argument "undo" (without quotes) 
 * this will remove last added waypoint 
6. If you run it with argument "reset" (without quotes) 
 * it will clear the LCD / Text panel 
7. You can add all those actions to toolbar in cockpit or remote 
8. Build timer block, set it to 5 seconds 
9. Setup timer block actions:  
 * 1. Run program block  
 * This will automatically add basic waypoint. Arguments do not work for Timer blocks (even tho they ask for it). 
 * You can override prefix by adding "PREFIX:My Prefix" to name of programmable block. (eg. Prog 1 PREFIX:Test) 
 * 2. Start timer 
10. Put timer start action and timer stop action to your toolbar  
 * this will allow you to start & stop automatic recording each 5 seconds 
11. Open control panel of LCD or Text Panel and click Edit Public Text to add the waypoints to your GPS. 
 
When you open the Public Text the waypoints will be automatically added to your GPS. 
You just need to 'Show on HUD' in GPS screen if you want to see them. 
 
Watch MMaster's Steam group: http://steamcommunity.com/groups/mmnews  
Twitter: https://twitter.com/MattsPlayCorner  
and Facebook: https://www.facebook.com/MattsPlayCorner1080p  
for more crazy stuff in the future :)  
*/ 
 
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! 
// DO NOT MODIFY ANYTHING BELOW THIS 
// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! 
void Configure() 
{ 
    // (for developer) Enable debug to antenna or LCD marked with [DEBUG] 
    MM.EnableDebug = false; 
} 
 
void Main(string argument) 
{ 
    Configure(); 
    // Init MMAPI and debug panels marked with [DEBUG] 
    MM.Echo = Echo; 
    MM.Me = Me; 
    MM.Init(GridTerminalSystem); 
 
    GpsRecorderProgram prog = new GpsRecorderProgram(); 
    prog.Run(argument); 
} 
} 
 
public class GpsRecorderProgram 
{ 
private IMyTerminalBlock ctrl; 
private string prefix = ""; 
 
public GpsRecorderProgram() 
{ 
    MM.Echo(""); 
    string myName = MM.Me.CustomName; 
    int nlIdx = myName.IndexOf("SOURCE:"); 
    int pfxIdx = myName.IndexOf("PREFIX:"); 
    string nameLike = (nlIdx < 0 || nlIdx + 7 >= myName.Length ? "" : myName.Substring(nlIdx + 7)); 
    if (nlIdx >= 0 && pfxIdx > nlIdx) 
    { 
        pfxIdx = nameLike.IndexOf("PREFIX:"); 
        prefix = (pfxIdx + 7 < nameLike.Length ? nameLike.Substring(pfxIdx + 7) : ""); 
        nameLike = nameLike.Substring(0, pfxIdx).Trim(); 
    } 
    else 
    { 
        prefix = (pfxIdx < 0 || pfxIdx + 7 >= myName.Length ? "" : myName.Substring(pfxIdx + 7)); 
        if (pfxIdx >= 0 && nlIdx > pfxIdx) 
        { 
            nlIdx = prefix.IndexOf("SOURCE:"); 
            prefix = prefix.Substring(0, nlIdx); 
        } 
    } 
 
    prefix = prefix.Trim(); 
    nameLike = nameLike.Trim(); 
 
    MMBlockCollection col = new MMBlockCollection(); 
    if (nameLike != "") 
    { 
        MM.Debug("INIT: Looking for block with name like '" + nameLike + "'.."); 
        col.AddBlocksOfNameLike(nameLike); 
 
        if (col.Count() <= 1) 
        { 
            MM.Debug("INIT: No block with name like '" + nameLike + "' found!"); 
            MM.Echo("ERROR: No block with name like '" + nameLike + "' found! Cannot add waypoints!"); 
            return; 
        } 
 
        ctrl = col.Blocks[0]; 
        // ignore this prog block 
        if (ctrl == MM.Me) 
            ctrl = col.Blocks[1]; 
 
        if (col.Count() > 2) 
        { 
            MM.Debug("INIT: " + (col.Count()-1).ToString() + " blocks found!"); 
        } 
 
        MM.Debug("INIT: Using block '" + ctrl.CustomName + "'."); 
    } 
    else 
    { 
        MM.Debug("INIT: Looking for remote control or cockpit.."); 
        col.AddBlocksOfType("remote"); 
 
        if (col.Count() <= 0) 
        { 
            col.AddBlocksOfType("cockpit"); 
            if (col.Count() <= 0) 
            { 
                MM.Debug("INIT: No cockpit or remote control found."); 
                MM.Echo("ERROR: No cockpit or remote control found! Cannot add waypoints!"); 
                return; 
            } 
 
            if (col.Count() > 1) 
                MM.Debug("INIT: " + col.Count().ToString() + " cockpits found."); 
 
            ctrl = col.Blocks[0]; 
            MM.Debug("INIT: Using cockpit '" + ctrl.CustomName + "'."); 
        } 
        else 
        { 
            if (col.Count() > 1) 
                MM.Debug("INIT: " + col.Count().ToString() + " remote controls found."); 
 
            ctrl = col.Blocks[0]; 
            MM.Debug("INIT: Using remote control '" + ctrl.CustomName + "'."); 
        } 
    } 
} 
 
public void Run(string argument) 
{ 
    argument = argument.Trim(); 
 
    int splitIdx = argument.IndexOf(" "); 
    string cmd = (splitIdx < 0 ? argument : argument.Substring(0, splitIdx)).ToLower(); 
    string arg = (splitIdx < 0 || splitIdx + 1 >= argument.Length ? "" : argument.Substring(splitIdx + 1)).Trim(); 
 
    MMBlockCollection panels = new MMBlockCollection(); 
    panels.AddBlocksOfType("textpanel", "RECORD"); 
    if (panels.Count() <= 0) 
    { 
        MM.Debug("RUN: No panel with RECORD in name found."); 
        MM.Echo("ERROR: No panel with RECORD in name found!"); 
        return; 
    } 
 
    for (int i = 0; i < panels.Count(); i++) 
        ProcessPanel(panels.Blocks[i] as IMyTextPanel, cmd, arg); 
} 
 
private void ProcessPanel(IMyTextPanel panel, string cmd, string arg) 
{ 
    string wp_text; 
    List<string> lines = new List<string>(panel.GetPublicText().Split('\n')); 
    // remove last empty line 
    if (lines.Count > 0) 
        lines.RemoveAtFast(lines.Count-1); 
    MM.Debug("PROC: Processing panel '" + panel.CustomName + "' cmd: '" + cmd + "' arg: '" + arg + "'"); 
     
    int idx = 1; 
    if (lines.Count > 0) 
    { 
        string last = lines[0]; 
        int wIdx = last.IndexOf(" Waypoint "); 
        string sub = (wIdx < 0 ? "" : last.Substring(wIdx + 10)); 
        int eIdx = sub.IndexOf(":"); 
        sub = (eIdx < 0 ? "" : sub.Substring(0, eIdx)); 
 
        if (!int.TryParse(sub, out idx)) 
            idx = lines.Count; 
        else 
            idx++; 
    } 
 
    switch (cmd) 
    { 
        case "reset": 
            panel.WritePublicText(""); 
            break; 
        case "undo": 
            if (lines.Count <= 0) 
                break; 
            lines.RemoveAt(0); 
            panel.WritePublicText(String.Join("\n", lines) + "\n"); 
            break; 
        case "": 
        case "add": 
            if (ctrl == null) 
                return; 
            VRageMath.Vector3D pos = ctrl.GetPosition(); 
 
            if (arg == "") 
                arg = prefix; 
            wp_text = "GPS:" + arg + " Waypoint " + idx.ToString("D3") + ":" +  
                    pos.GetDim(0).ToString("F2") + ":" + 
                    pos.GetDim(1).ToString("F2") + ":" + 
                    pos.GetDim(2).ToString("F2") + ":"; 
            panel.WritePublicText(wp_text + "\n" + String.Join("\n", lines) + "\n"); 
            break; 
        /*case "addfront": 
            if (ctrl == null) 
                return; 
 
            float dist; 
            if (arg == "") 
            { 
                dist = 100; 
                arg = prefix; 
            } 
            else 
            { 
                int pIdx = arg.IndexOf(" "); 
                string strNum = (pIdx < 0 ? arg : arg.Substring(0, pIdx)); 
                if (!float.TryParse(strNum, out dist)) 
                { 
                    MM.Echo("ERROR: Invalid input 'addfront " + arg + 
                            "'. Expecting: 'addfront <distance in m> <prefix>'"); 
                    return; 
                } 
                arg = (pIdx < 0 || pIdx + 1 >= arg.Length ? prefix : arg.Substring(pIdx + 1)); 
            } 
 
            VRageMath.Matrix o, t = VRageMath.Matrix.CreateFromDir(VRageMath.Vector3.Forward * dist); 
            ctrl.Orientation.GetMatrix(out o); 
            o *= t; 
            Vector3 v = o.GetDirectionVector(Base6Directions.Direction.Forward); 
            wp_text = "GPS:" + arg + " Waypoint " + idx.ToString("D3") + ":" +  
                    v.GetDim(0).ToString("F2") + ":" + 
                    v.GetDim(1).ToString("F2") + ":" + 
                    v.GetDim(2).ToString("F2") + ":"; 
            panel.WritePublicText(wp_text + "\n" + String.Join("\n", lines) + "\n"); 
            break;*/ 
        default: 
            MM.Echo("ERROR: Unknown argument '" + cmd + "'. Use one of these arguments to Run: 'add <name>' or 'undo' or 'reset'"); 
            MM.Debug("PROC: Unknown argument '" + cmd + "'"); 
            break; 
    } 
 
    MM.Debug("PROC: Done."); 
    panel.ShowTextureOnScreen(); 
    panel.ShowPublicTextOnScreen(); 
} 
} 
 
// MMAPI below (do not modify)   
// IMyTerminal collection with useful methods   
public class MMBlockCollection 
{ 
public List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>(); 
 
// add Blocks with name containing nameLike   
public void AddBlocksOfNameLike(string nameLike) 
{ 
    if (nameLike == "" || nameLike == "*") 
    { 
        List<IMyTerminalBlock> lBlocks = new List<IMyTerminalBlock>(); 
        MM._GridTerminalSystem.GetBlocks(lBlocks); 
        Blocks.AddList(lBlocks); 
        return; 
    } 
 
    string group = (nameLike.StartsWith("G:") ? nameLike.Substring(2).Trim().ToLower() : ""); 
    if (group != "") 
    { 
        List<IMyBlockGroup> BlockGroups = new List<IMyBlockGroup>(); 
        MM._GridTerminalSystem.GetBlockGroups(BlockGroups); 
        for (int i = 0; i < BlockGroups.Count; i++) 
        { 
            IMyBlockGroup g = BlockGroups[i]; 
            if (g.Name.ToLower() == group) 
                g.GetBlocks(Blocks); 
        } 
        return; 
    } 
 
    MM._GridTerminalSystem.SearchBlocksOfName(nameLike, Blocks); 
} 
 
// add Blocks of type (optional: with name containing nameLike)   
public void AddBlocksOfType(string type, string nameLike = "") 
{ 
    if (nameLike == "" || nameLike == "*") 
    { 
        List<IMyTerminalBlock> blocksOfType = new List<IMyTerminalBlock>(); 
        MM.GetBlocksOfType(ref blocksOfType, type); 
        Blocks.AddList(blocksOfType); 
    } 
    else 
    { 
        string group = (nameLike.StartsWith("G:") ? nameLike.Substring(2).Trim().ToLower() : ""); 
        if (group != "") 
        { 
            List<IMyBlockGroup> BlockGroups = new List<IMyBlockGroup>(); 
            MM._GridTerminalSystem.GetBlockGroups(BlockGroups); 
            for (int i = 0; i < BlockGroups.Count; i++) 
            { 
                IMyBlockGroup g = BlockGroups[i]; 
                if (g.Name.ToLower() == group) 
                { 
                    List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>(); 
                    g.GetBlocks(groupBlocks); 
                    for (int j = 0; j < groupBlocks.Count; j++) 
                        if (MM.IsBlockOfType(groupBlocks[j], type)) 
                            Blocks.Add(groupBlocks[j]); 
                    return; 
                } 
            } 
            return; 
        } 
        List<IMyTerminalBlock> blocksOfType = new List<IMyTerminalBlock>(); 
        MM.GetBlocksOfType(ref blocksOfType, type); 
 
        for (int i = 0; i < blocksOfType.Count; i++) 
            if (blocksOfType[i].CustomName.Contains(nameLike)) 
                Blocks.Add(blocksOfType[i]); 
    } 
} 
 
// add all Blocks from collection col to this collection   
public void AddFromCollection(MMBlockCollection col) 
{ 
    Blocks.AddList(col.Blocks); 
} 
 
// clear all reactors from this collection   
public void Clear() 
{ 
    Blocks.Clear(); 
} 
 
// number of reactors in collection   
public int Count() 
{ 
    return Blocks.Count; 
} 
} 
 
// MMAPI Helper functions   
public static class MM 
{ 
public static bool EnableDebug = false; 
public static IMyGridTerminalSystem _GridTerminalSystem = null; 
public static IMyProgrammableBlock Me; 
public static Action<string> Echo; 
public static MMBlockCollection _DebugTextPanels = null; 
public static Dictionary<string, Action<List<IMyTerminalBlock>>> BlocksOfStrType = null; 
 
public static void Init(IMyGridTerminalSystem gridSystem) 
{ 
    _GridTerminalSystem = gridSystem; 
    _DebugTextPanels = new MMBlockCollection(); 
 
    // prepare debug panels 
    // select all text panels with [DEBUG] in name  
    if (EnableDebug) 
    { 
        _DebugTextPanels.AddBlocksOfType("textpanel", "[DEBUG]"); 
        Debug("DEBUG Panel started.", false, "DEBUG PANEL"); 
    } 
} 
 
public static double GetPercent(double current, double max) 
{ 
    return (max > 0 ? (current / max) * 100 : 100); 
} 
 
public static string GetBlockTypeDisplayName(IMyTerminalBlock block) 
{ 
    return block.DefinitionDisplayNameText; 
} 
 
public static void GetBlocksOfExactType(ref List<IMyTerminalBlock> blocks, string exact) 
{ 
    if (exact == "TextPanel") _GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(blocks); 
    else 
    if (exact == "Cockpit") _GridTerminalSystem.GetBlocksOfType<IMyCockpit>(blocks); 
    else 
    if (exact == "RemoteControl") _GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(blocks); 
    else 
    if (exact == "ShipController") _GridTerminalSystem.GetBlocksOfType<IMyShipController>(blocks); 
} 
 
public static void GetBlocksOfType(ref List<IMyTerminalBlock> blocks, string typestr) 
{ 
    typestr = typestr.Trim().ToLower(); 
 
    GetBlocksOfExactType(ref blocks, TranslateToExactBlockType(typestr)); 
} 
 
public static bool IsBlockOfType(IMyTerminalBlock block, string typestr) 
{ 
    return block.BlockDefinition.ToString().Contains(TranslateToExactBlockType(typestr)); 
} 
 
public static string TranslateToExactBlockType(string typeInStr) 
{ 
    typeInStr = typeInStr.ToLower(); 
 
    if (typeInStr.StartsWith("text") || typeInStr.StartsWith("lcd")) 
        return "TextPanel"; 
    if (typeInStr.StartsWith("coc")) 
        return "Cockpit"; 
    if (typeInStr.StartsWith("remote")) 
        return "RemoteControl"; 
    if (typeInStr.StartsWith("controller")) 
        return "ShipController"; 
    return "Unknown"; 
} 
 
public static string FormatLargeNumber(double number, bool compress = true) 
{ 
    if (!compress) 
        return number.ToString( 
            "#,###,###,###,###,###,###,###,###,###"); 
 
    string ordinals = " kMGTPEZY"; 
    double compressed = number; 
 
    var ordinal = 0; 
 
    while (compressed >= 1000) 
    { 
        compressed /= 1000; 
        ordinal++; 
    } 
 
    string res = Math.Round(compressed, 1, MidpointRounding.AwayFromZero).ToString(); 
 
    if (ordinal > 0) 
        res += " " + ordinals[ordinal]; 
 
    return res; 
} 
 
public static void WriteLine(IMyTextPanel textpanel, string message, bool append = true, string title = "") 
{ 
    textpanel.WritePublicText(message + "\n", append); 
    if (title != "") 
        textpanel.WritePublicTitle(title); 
    textpanel.ShowTextureOnScreen(); 
    textpanel.ShowPublicTextOnScreen(); 
} 
 
public static void Debug(string message, bool append = true, string title = "") 
{ 
    if (!EnableDebug) 
        return; 
 
    DebugTextPanel(message, append, title); 
} 
 
 
public static void DebugTextPanel(string message, bool append = true, string title = "") 
{ 
    for (int i = 0; i < _DebugTextPanels.Count(); i++) 
    { 
        IMyTextPanel debugpanel = _DebugTextPanels.Blocks[i] as IMyTextPanel; 
        debugpanel.SetCustomName("[DEBUG] Prog: " + message); 
        WriteLine(debugpanel, message, append, title); 
    } 
} 
