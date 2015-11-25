#Welcome to nSwagger

nSwagger is a .NET codegen for [Swagger 2.0](http://swagger.io) built using the [Roslyn](https://github.com/dotnet/roslyn) compiler and aims to provide a number of pieces of functionality.

## Core library

The core of the project is a library which will allow you to pass in URLs or file paths to Swagger definations and have that generate a C# file interface to connect to make the calls to it.   
Currently this library works for most simple specifications and produces code that can be used to make calls to a Swagger endpoint.

## Console 

The first client for the core library is a console application which you can run and produce the code from the Swagger files.  
Currently this exists but isn't really usable yet.

## Visual Studio Integration

The second client is to add an extension to Visual Studio, the goal of this is to be able to right click a project and have "Add Swagger Reference" appear, which will let you easily consume a Swagger endpoint.

## Limitations

###JSON only
The endpoint must support JSON, as this is all that is supported by the library.

###Not support for HEAD, OPTIONS &amp; PATCH
The only HTTP methods currently supported are POST, PUT, DELETE and GET. Swagger makes allowance for HEAD, OPTIONS and PATCH too and we still need to add those in.

###Validation
The Swagger defination provides a LOT of information for validating the data. It would be great to have the data provided to the calls validated client side before the call is made.

# Want to help?
We would love you help with this project! So please feel free to fork the code and do a pull request!   
Important to note is our [Code License (MIT)](LICENSE.md) and our [Contributor License Agreement](Contributor-License-Agreement.md).