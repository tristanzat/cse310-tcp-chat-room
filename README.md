# Overview

This software is a client-to-server TCP chat room. It handles multiple clients connecting to a single server. Opening this repository in Visual Studio should* allow you to run both the server and a client simultaneously (either by clicking Start or pressing F5).

*I don't know if `TCPChatRoom.slnLaunch` automatically adds itself as a startup project or not

I wrote this software with the intent of learning how networking works.

[Software Demo Video](http://youtube.com)

# Network Communication

This uses a client/server architecture; the server hosts the chat room for clients to connect to.

This chat room uses TCP. Uses port 5000.

The messages are newline-delimited UTF-8 text.

# Development Environment

I used Visual Studio Community Edition 2026 to develop this software.

The server and client code is written in C#. This makes use of the `TcpClient` and `TcpListener` helper classes.

# Useful Websites

* [Use TcpClient and TcpListener - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/tcp-classes)
* [C# Networking: Raw Sockets, TCP, and UDP Programming - DEV Community](https://dev.to/chakewitz/c-networking-raw-sockets-tcp-and-udp-programming-46oc)

# Future Work

* Item 1
* Item 2
* Item 3
