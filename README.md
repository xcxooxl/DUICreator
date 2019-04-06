# DUICreator
Creates awesome DUI inside GTA ! Easily!

Example: 

![alt text](https://i.imgur.com/x6ZhbZg.png)

You can check example code in Example.cs

DUIHandler exposes 3 ways to create duis
```
Exports.Add("createDui", new Func<string, string, Task<DuiContainer>>(AddDui));
Exports.Add("CreateRandomUniqueDuiContainer", new Func<string, Task<DuiContainer>>(CreateRandomUniqueDuiContainer));
Exports.Add("destroyAllDui", new Func<Task>(DestroyAllDui));

AddDui(String renderTarget, string url)
CreateRandomUniqueDuiContainer(url) Creates a random DUI for this url with unique renderTarget
destroyAllDui Removes all DUI without destroying for reuseability !!
```
