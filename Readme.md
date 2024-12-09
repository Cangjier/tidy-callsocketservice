# CallSocketService

调用Socket服务接口的命令行工具。当服务程序未启动，则会自动启动服务程序。

``` bat
CallSocketService.exe call -s {ServerName} -n {InterfaceName} -as true -i {InputPath} -o {OutputPath} -l {LoggerPath}
```

- -s : Server Name, such as NX/SolidWorks/Creo/Inventor/Revit/Catia/AutoCad/ZWCad
- -n : Interface Name, such as NXOpenFile/NXInsertComponent
- -i : Input Path, such as "C:\Temp\Input.json"
- -o : Output Path, such as "C:\Temp\Output.json"
- -l : Logger Path, such as "C:\Temp\Logger.txt"
- -as : Auto Start, such as true/false


## 服务程序配置
如果服务程序未配置，则当获取可用服务端口失败时，会抛出异常。
当前服务程序配置在config.json中，如下所示：
``` json
{
    "NX":{
        "Path":"$env(`UGII_BASE_DIR`)+`/NXBIN/ugraf.exe`",
        "Arguments":"-nx",
        "WorkingDirectory":"$env(`UGII_BASE_DIR`)+`/UGII/`",
        "Install":"NXOpenFile.exe /install",
        "Timeout":30000,
    }
}
```

# Roadmap

1. 
