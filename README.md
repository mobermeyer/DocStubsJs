# DocStubsJs
An extension for Visual Studio 2012+ to auto generate documentation comment stubs for JSDoc and VSDoc in JavaScript and TypeScript editors.


## Contributing

In order to run Visual Studio's plugin development environment for this extension, you will need Visual Studio 2012 Professional (Ultimate or Premium work, too) and its [SDK](https://www.microsoft.com/en-us/download/details.aspx?id=30668) installed.

### How it works

DocStubsJs is a fairly straight-forward plugin that uses events and string parsing to determine when and how to insert a document stub. It essentially just watches key-strokes for the identifying documentation comment bock initializers. ("///" for VSDoc, and "/**" for JSDoc) Once it has determined that these have been entered on an empty line, then it parses the lines surrounding it to determine whether to add a new comment line, create a documentation comment stub, or do nothing.
