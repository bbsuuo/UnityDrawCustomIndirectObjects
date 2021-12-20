# UnityDrawCustomIndirectObjects
演示关于如何渲染几十万量级相同对象
1.实现了纯GPU的渲染流程
2.实现视锥剔除算法(Computer Shader)
3.实现了HZ算法
4.可以自定义网格和Shader,并且支持ASE的Shader( ASE 需要自定义编辑模板文件,模板文件我就不传了 把vertex方法改一下instanceid就行)


要导入URP管线, 然后将管线文件配置好就可以了
