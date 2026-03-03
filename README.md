# Unity Shader Source Lifting for Customisation

code to compile as console app (.NET 10).  
  
can uplift a Unity Shader source to have shaders renamed to having a custom prefix and reference their own set of Unitys CGinc files.  
  
shader and "CGinc" files will be copied modified together in 1 output folder to cater for known issues in Unity like that ComputeShaders and their CGinc must reside in the same folder.