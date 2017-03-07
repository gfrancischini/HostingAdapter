# Hosting Adapter
Library Helper to implement communication using stdio, tcp socket or any communication
 protocol that implements the IMessageReader and IMessageWriter interfaces

## Message Dispatcher

Messages are read from IMessageReader\IMessageWriter and serialized\deserialized using the IMessageSerializer
Message handlers are registered with the dispatcher.  Ass the messages are processed by
the dispatcher queue they are routed to any registered handlers.  The dispatch queue processes messages 
serially.

Messages are handled by services that register with the message processing component. The message dispatch table maps
message names to callbacks in the various services. Types are converted on the MessageDispatcher from and to JSON.

## Channel Impementation

Currently this hosting service implements the stdio server and client and the TCP Socket Server. 
Its also possible to create new channels by extending the class Channel.

## Serialization\Deserialization Implementation

You can extend this hosting service to implement any communication protocol by implementing the interfaces:
* IMessageReader
* IMessageWriter
* IMessageSerializer


This project is highly based on the [Microsoft Sql Tools Service](https://github.com/Microsoft/sqltoolsservice).