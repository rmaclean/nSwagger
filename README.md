[![Build status](https://ci.appveyor.com/api/projects/status/691q7f5xj2xbmu7p?svg=true)](https://ci.appveyor.com/project/rmaclean/nswagger)

# Downloads
[Download Console Application](https://ci.appveyor.com/api/buildjobs/hv3nochf0ve7cua4/artifacts/nSwagger.Console/bin/consoleAppVeyor.zip)  
[Download WPF Application](https://ci.appveyor.com/api/buildjobs/va8unrgrluc815nf/artifacts/nSwagger.GUI/bin/wpfAppVeyor.zip)

# Welcome to nSwagger

nSwagger is a set of tools for developers who use Microsoft languages (C# & TypeScript) and tools to have an easy experience with APIs which are documented with [Swagger 2.0](http://swagger.io).   

For **C#**, nSwagger offers a codegen tool built using the [Roslyn](https://github.com/dotnet/roslyn) compiler and aims to provide a number of pieces of functionality.  
For **TypeScript** as there is already a [great JS codegen](https://github.com/wcandillon/swagger-js-codegen) we are focused on generating definations to make it easier to work with from TypeScript.

![Example Image](https://raw.githubusercontent.com/rmaclean/nSwagger/master/Assets/example.jpg)

## Core library

The core of the project is a library which will allow you to pass in URLs or file paths to Swagger definations and have that generate a static type interface which can then be used to generate other outputs.

### C&#35;
Included in the Core Library is also a C# Generator which takes the Swagger defination and produces C# code which can call the API endpoint.

### TypeScript
Included in the Core Library is also a TypeScript defination generator which takes the Swagger defination for requests & responses and produces interfaces and classes that augment the JS codegen for Swagger.

## Clients
To make it easier to work with for developers we are building a number of clients which will expose the functionality. 

### Console 

The first client for the core library is a console application which you can run and produce the code from the Swagger files.  

### WPF
A GUI client which offers similar functionality to the console client but with a GUI.

### Visual Studio Integration

The ultimate client is to add an extension to Visual Studio, the goal of this is to be able to right click a project and have "Add Swagger Reference" appear, which will let you easily consume a Swagger endpoint.

## Limitations

These are items we plan on resolving asap.

### Stability of generated code
As we figure out the right structures the generated code may change between values.

### JSON only
For C#, the endpoint must support JSON requests/responses, as this is all that is supported by the library.

### Validation
For C#, the Swagger defination provides a LOT of information for validating the data. It would be great to have the data provided to the calls validated client side before the call is made.

### OAuth Only
For C#, The only authentication currently supported is OAuth.

# Want to help?
We would love you help with this project! So please feel free to fork the code and do a pull request!   
Important to note is our [Code License (MIT)](LICENSE.md) and our [Contributor License Agreement](Contributor-License-Agreement.md).