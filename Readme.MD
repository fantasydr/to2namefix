皇家骑士团-命运之轮  
旧版汉化版人物名称修正  
（v1.1 to 典藏版）  

此工具适用于修复2012年版的皇家骑士团v1.1存档。  
2016年7月14日汉化典藏版发布之后，由于码表改变，读取旧存档人物名称变乱码。  

【使用方法】  
1、需要PSP上安装CheatManager  
2、打开典藏版ISO，载入旧存档。（顺道说，5.50 prome-4需要用isotool打补丁才可以正常游戏）  
3、在大地图上，进入编成界面，按PSP'音乐'符号呼出CheatManager  
4、选择'记忆体管理'->'Dump记忆体'  
5、稍等片刻后会把内存导出到文件。  
6、不用退出游戏，连接USB线，选择USB连接，这时电脑应该可以读取PSP记忆棒。  
7、在记忆卡目录CheatManager\Mem\ 文件夹中应该有dump出的内存文件。  
8、把最新的那个拷贝到此工具目录。一般文件名为0x0000000_0x1800000_**.mem  
9、将此*.mem文件改名为dump.mem  
10、运行工具TO2NameFix.exe，一切顺利的话会出现ULJM-05753_NameFix.CMF  
11、将这个CMF文件拷贝到记忆卡CheatManager\CMF\ 文件夹  
12、在PSP上按x键断开USB连接，这时CheatManager可以读取CMF文件。  
13、选择载入表格->清除表格，保证没有其他代码运行。  
14、选择载入表格->读取CMF，加载刚刚拷贝来的CMF  
15、选择位置表格，应该可以看到多出一项Fix Name  
16、按三角键，执行此项CMF，选择是。  
17、按X键两次，回到游戏，理论上所有人物名称已经被修复。  
18、保存当前游戏到新的存档，这个存档将可以在典藏版显示正确人名。  

感谢汉化组4年之后还能补完皇骑的汉化。  
也希望大家支持PSP正版游戏。  

【原理】  
TO2_encode.txt是WGF大大提供的v1.1码表。  
TO2_encode_new.txt是我从典藏版拿到的新码表。  
CheatManager可以dump当前内存，里面包括所有名字信息。  
军团名地址：0x2D4CBC  
英雄地址：0x2D4CD5  
军团成员名称地址起始：0x2D4CFC  
每个成员偏移0x48C字节  

此程序根据旧码表分析出人物姓名，然后用新码表生成CMF金手指文件。  
加载并执行金手指，则可以把名称修复。  
如果名称中有不认识的字符，会变成问号。  

https://github.com/fantasydr/to2namefix  