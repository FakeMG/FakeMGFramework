- SO event system (in the inspector)
   delay events (done)
   event sequence: wait for previous events (shouldn't implement this)
   payload adapter (done)
   can order the listeners
   manage events tool (done)
   hard to debug
   lots of reference in the editor: Find Reference 2
   editor button to manually trigger event (done)
   AI cannot gen / cannot be reviewed in PR
   less performance
   easy to change subscribers and callers
   conflict scene
Runtime created objects will subscribe through code
Others will reference in the inspector
Only hook event in the inspector to
	systems that can not access the data sent through the event by itself
	systems that need data as soon as possible