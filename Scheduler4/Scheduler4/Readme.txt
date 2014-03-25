Copyright Philip Pierce 2014


This scheduler is a useful tool for maintaining state and scheduling future
events / actions in multiplayer game environments or any situation which needs
millisecond control of events with high bandwidth and multi-threaded capabilities.

This scheduler has been used in a few of the MMO games I've worked on. It runs
best on a server, and is used for dispatching messages (Chat systems), scheduling 
player vs NPC combat actions, and any other function that requires scheduled events.

The included console app demo shows how easy it is to create a scheduler. Typically,
you will have one scheduler running for the whole realm / server. Then, you create an
IExecutableWorkItem for each type of scheduled task you want to track.

