# CallSocketService

����Socket����ӿڵ������й��ߡ����������δ����������Զ������������

``` bat
CallSocketService.exe call -s {ServerName} -n {InterfaceName} -as true -i {InputPath} -o {OutputPath} -l {LoggerPath}
```

- -s : Server Name, such as NX/SolidWorks/Creo/Inventor/Revit/Catia/AutoCad/ZWCad
- -n : Interface Name, such as NXOpenFile/NXInsertComponent
- -i : Input Path, such as "C:\Temp\Input.json"
- -o : Output Path, such as "C:\Temp\Output.json"
- -l : Logger Path, such as "C:\Temp\Logger.txt"
- -as : Auto Start, such as true/false


## �����������
����������δ���ã��򵱻�ȡ���÷���˿�ʧ��ʱ�����׳��쳣��
��ǰ�������������config.json�У�������ʾ��
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
