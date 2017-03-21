n00bKeper Drone (Last Updated: 3/2/2017)

Drone program designed to implement machine learning:reinforcement learning and Goal Orientated Action Planning to allow the AI to "choose" how best to act in Space Engineers based on varying imput.

Was insipred by a reddior (https://www.reddit.com/r/spaceengineers/comments/3y1986/virus_ai_scripts_suggestions) who was looking to create a self replicating drone system to allow for more challenge in survival.  

Machine learning is a type of artificial intelligence (AI) that provides computers with the ability to learn without being explicitly programmed. Machine learning focuses on the development of computer programs that can change when exposed to new data.

Goal Oriented Action Planning (GOAP) is an AI system that will easily give your agents choices and the tools to make smart decisions without having to maintain a large and complex finite state machine.

The Current AI (3/2/2017) has a set of pre-programed methods that it "chooses" from but has very basic logic of:

1) Initialize Script
2) Check for Players Near
	a) If player found => attack
		- If weapons available => shoot
			-if weapon is empty => return to origin
		- else => return to origin
	b) else => return to origin

I would like to expand the AI's descision making as well as add additional "Tasks" that can be given to the drone.

Example Future Tasks:
	[] Recon
	[] Mining
	[] Clean-up (eat ships) 
	[] Building

---------- Versions -----------
Version 0.1:
	- Goals for v0.1:
		[] Drone that Patrols Waypoints
			[] Generates Circular Waypoints if None Exist (Default 5km)
		[] Drone reacts to damage
		[] Improve Drone Combat Strategies
			- Strafing
			- 3D escape
	- 3D Escape:
		- Current remote control AutoPilot functionality has a Remote Control reach it's destination, then change its orientation before
		contiunuing onto it's next waypoint.  These pauses in movement are costly and can cost a drone it's life (negative poor machine 
		learning success score).
		- 3D Escape is a concept to allow the AI to calculate a turning radius to make a manuver as well as better understanding of how to
		use the inertial dampeners to it's benifit.
			{Needs Testing} - Turning around 180 degrees is an inefficent use of momentum.  By calculating and implimenting a turning radius as a list of waypoints
			the AI can turn around while still remaining on the move to avoid 
			- Players can turn off dampeners and coast as they orientate their ship.  This allows them to remain a moving target and harder to hit
	- Process':
		[] Patrol Waypoints:
			1) Get Waypoints
			2) Get First Waypoint in List
			3) Set Waypoint in Remote Control
			4) Enable AutoPilot
			5) Pop waypoint from list and push to end
			6) Loop Commands:
				- Check Goals:
					1) Survive
					2) Patrol Area
					3) Record New Information
		[] Event Evaluation
			1) 