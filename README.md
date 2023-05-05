# KubeMQContract
The idea behind KubeMQContract is to wrap the interactions with a KubeMQ server in a simple and easy to use interface.  
This is done through defining Messages (classes) and tagging them appropriately as necessary, then using those to interact with KubeMQ.  Using this concept 
you can create a general library that contains all your Message classes (contract definitions) that are expected to be transmitted and recieved within a system.
In addition to this, there is also the ability to specify versions for a given message type and implementing converters for original message types to the new 
types.  This is in line with the idea of not breaking a contract within a system by updating a listener and giving time to other developers/teams to update 
their systems to supply the new version of the message.  By default all message body's are json encoded and unencrypted, all of which can be overridden on a 
global level or on a per message type level through implementation of the appropriate interfaces.
