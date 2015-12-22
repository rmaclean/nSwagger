[![Build status](https://ci.appveyor.com/api/projects/status/691q7f5xj2xbmu7p?svg=true)](https://ci.appveyor.com/project/rmaclean/nswagger)

# Latest Download
You can get the latest WPF &amp; console client downloads (&amp; all historic ones too) in **[Releases](https://github.com/rmaclean/nSwagger/releases)**.

# Welcome to nSwagger

nSwagger is a set of tools for developers who use Microsoft languages (C# & TypeScript) which aim to ease the experience with REST APIs documented using [Swagger 2.0](http://swagger.io).   

For **C#**, nSwagger offers a codegen tool built using the [Roslyn](https://github.com/dotnet/roslyn) compiler and aims to produce a full client library for as the output.  
For **TypeScript** as there is already a [great JS codegen](https://github.com/wcandillon/swagger-js-codegen), we are focused on generating definations to make it easier to work with the JS from the codegen while working inside of TypeScript.

![Example Image](https://raw.githubusercontent.com/rmaclean/nSwagger/master/Assets/example.jpg) 

You can see some examples of the outputs in our [Examples](examples) folder.

## Core Library

The core of the project is a library which will allow you to pass in URLs or file paths to Swagger definations and have that generate a C# class reflecting the defination (this is called the `SpecificationClass`), which can then be used to generate other outputs. 

### C&#35;
Included in the Core Library is also a C# Code Generator which takes the `SpecificationClass` and produces C# file, which can call the API endpoint.

### TypeScript
Included in the Core Library is also a TypeScript defination generator which takes the `SpecificationClass` which add to the JS codegen for Swagger by providing a TypeScript definations for the requests & responses.

## Clients
To make it easier to work with for developers we are building a number of clients which will expose the functionality. Ideally you should just download the client of your choice and generate the files you need.

### Console 
The first client for the core library is a console application which you can run and produce the code from the Swagger files. This is available now in the Releases.

### WPF
A GUI client which offers similar functionality to the console client but with a GUI. This is available now in the Releases.

### Visual Studio Integration

The ultimate client is to add an extension to Visual Studio, the goal of this is to be able to right click a project and have "Add Swagger Reference" appear, which will let you easily consume a Swagger endpoint.

## Requirements
### Core Library
If you are going to consume the core library or the C# code gen, you will need to add Using this class requires the use of [JSON.NET](http://www.newtonsoft.com/json) to your project.  

### Clients
If you are just using the client then you only need [.NET 4.6.1](http://smallestdotnet.com/) (or later) installed.

## Limitations

These are items we plan on resolving asap.

### Stability of generated code
As we figure out the right structures the generated code may change between values.

### JSON only
For C#, the endpoint must support JSON requests/responses, as this is all that is supported by the library.

### Validation
For C#, the Swagger defination provides a LOT of information for validating the data. It would be great to have the data provided to the calls validated client side before the call is made.

### OAuth2 Only
For C#, The only authentication currently supported is OAuth2.

# Want to help?
We would love you help with this project! So please feel free to fork the code and do a pull request!   
Important to note is our [Code License (MIT)](LICENSE.md) and our [Contributor License Agreement](Contributor-License-Agreement.md).