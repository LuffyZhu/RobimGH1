<<<<<<< HEAD


Grasshopper plugin for programming ABB, KUKA, UR ,Staubli robots and AUBO, FANUC

### 官方
* 最新发布版本： [latest release](https://github.com/visose/Robots/releases).
* 模型库：  [robot library](https://github.com/visose/Robots/wiki/Robot-libraries).
* Download the [example files](https://github.com/visose/Robots/tree/master/Documentation/Examples) (they work with the Bartlett robot library).
* Read the [Wiki](https://github.com/visose/Robots/wiki).

### 大界
* 最新版本： \\ROBOTICPLUS\share\045-RobotsRP\build
* 模型库： \\ROBOTICPLUS\share\045-RobotsRP\model

### 安装方式

#### 直接使用编译好的文件

1. 从官网或者大界网盘下载 .dll文件和.gha文件，全部复制放到 grasshopper 的 Libraries 下，如`C:\Users\username\AppData\Roaming\Grasshopper\Libraries`

2. 在 `C:\Users\username\Documents\` 目录下新建一个`Robots`文件夹，将下载的模型库放进去，如下图所示：

<img src="https://qipccc-alipic.oss-cn-shanghai.aliyuncs.com/images/20200318184223.png"/>

#### 使用源码编译安装

+ 环境：

    + visual studio 2019
    + Microsoft .NET FrameWork 4.7 
    + Grasshopper templates for v6
1. 下载源码，使用文本编辑器打开`RobotsGH`目录下的`RobotsGH.csproj`文件，修改`<OutputPath>`中的程序生成目录

2. 使用vs2019打开`Robots.sln`文件，点击 生成 -> 生成解决方案，在上面设置的目录下得到程序生成文件
=======
# RobimGH1
avoid destroying robimedu
>>>>>>> 1c6f650ff00320d8c5fc5f47a57d07408ee3da5d
