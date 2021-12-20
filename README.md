# UnityDrawCustomIndirectObjects
演示关于如何渲染几十万量级相同对象
1.实现了纯GPU的渲染流程
2.实现视锥剔除算法(Computer Shader)
3.实现了HZ算法
4.可以自定义网格和Shader,并且支持ASE的Shader( ASE 需要自定义编辑模板文件,模板文件我就不传了 把vertex方法改一下instanceid就行)

代码仅供参考,由于我的项目需求不一样, hz算法部分只进行了实现,如果要加入工作流可能需要你自己再进行一定的更改

要导入URP管线, 然后将管线文件配置好就可以了
![image](https://user-images.githubusercontent.com/35555275/146745381-5862a939-9abb-4ac6-9b1b-2f2c7966b4c2.png)
![image](https://user-images.githubusercontent.com/35555275/146745425-cb3e8d8e-2ce9-4f0c-8f4d-8788a571490d.png)
![image](https://user-images.githubusercontent.com/35555275/146745455-e6c3d8a0-c92e-4dd3-8538-a806300f3fa5.png)

部分实现参考了： https://github.com/ellioman/Indirect-Rendering-With-Compute-Shaders 和 https://zhuanlan.zhihu.com/p/396979267#comment-10019966137?notificationId=1455556800368558080
