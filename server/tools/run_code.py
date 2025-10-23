"""
Unity代码运行工具，包含Python代码执行和C#代码编译执行功能。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_run_code_tools(mcp: FastMCP):
    @mcp.tool("python_runner")
    def python_runner(
        ctx: Context,
        action: Annotated[str, Field(
            title="Python操作类型",
            description="要执行的Python操作: execute(执行代码), validate(验证代码), install_package(安装包)",
            examples=["execute", "validate", "install_package"]
        )],
        code: Annotated[Optional[str], Field(
            title="Python代码",
            description="要执行或验证的Python代码，支持UTF-8编码",
            default=None,
            examples=[
                "print('Hello Unity!')",
                "import numpy as np\nprint(np.array([1,2,3]))",
                "# 创建3D模型\nimport trimesh\nmesh = trimesh.creation.box()"
            ]
        )] = None,
        description: Annotated[Optional[str], Field(
            title="功能描述",
            description="脚本功能的详细描述",
            default=None,
            examples=["数据处理脚本", "图像转换工具", "3D模型生成器"]
        )] = None,
        package_name: Annotated[Optional[str], Field(
            title="包名称",
            description="要安装的Python包名称，仅在install_package操作时使用",
            default=None,
            examples=["numpy", "trimesh", "matplotlib", "opencv-python"]
        )] = None,
        version: Annotated[Optional[str], Field(
            title="包版本",
            description="要安装的包版本号，留空安装最新版本",
            default=None,
            examples=["1.21.0", ">=1.0.0", "~=2.1.0"]
        )] = None,
        timeout: Annotated[Optional[int], Field(
            title="超时时间",
            description="代码执行或包安装的超时时间（秒）",
            default=30,
            ge=1,
            le=300
        )] = 30,
        cleanup: Annotated[bool, Field(
            title="自动清理",
            description="执行完成后是否自动清理临时文件和变量",
            default=True
        )] = True
    ) -> Dict[str, Any]:
        """Unity Python代码运行工具，支持执行Python代码、验证语法和安装包。

        提供完整的Python运行环境，适用于：
        - 数据处理：使用NumPy、Pandas等库处理数据
        - 3D建模：使用Trimesh、Open3D等库创建模型
        - 图像处理：使用OpenCV、PIL等库处理图像
        - 机器学习：运行TensorFlow、PyTorch等模型

        
        """
        
        return send_to_unity("python_runner", {
            "action": action,
            "code": code,
            "description": description,
            "package_name": package_name,
            "version": version,
            "timeout": timeout,
            "cleanup": cleanup
        })


    @mcp.tool("code_runner")
    def code_runner(
        ctx: Context,
        action: Annotated[str, Field(
            title="C#操作类型",
            description="要执行的C#操作: execute(编译执行), validate(验证语法)",
            examples=["execute", "validate"]
        )],
        code: Annotated[str, Field(
            title="C#代码",
            description="要编译执行或验证的C#代码，支持完整的Unity API访问。支持顶层语句和完整类定义",
            examples=[
                "Debug.Log(\"Hello from C#!\");",
                "var go = new GameObject(\"TestObject\"); go.transform.position = Vector3.zero;",
                "var terrain = GameObject.Find(\"Terrain\").GetComponent<Terrain>();\nterrain.terrainData.SetHeights(0, 0, new float[513, 513]);"
            ]
        )],
        description: Annotated[Optional[str], Field(
            title="功能描述",
            description="代码功能的详细描述",
            default=None,
            examples=["场景对象创建", "UI组件管理", "地形编辑工具"]
        )] = None,
        class_name: Annotated[Optional[str], Field(
            title="类名",
            description="类名，默认为CodeClass",
            default=None,
            examples=["CodeClass", "TerrainHelper", "MeshGenerator"]
        )] = None,
        entry_method: Annotated[Optional[str], Field(
            title="入口方法名",
            description="入口方法名称，默认为Run",
            default=None,
            examples=["Run", "Execute", "Main"]
        )] = None,
        namespace: Annotated[Optional[str], Field(
            title="命名空间",
            description="命名空间，默认为CodeNamespace",
            default=None,
            examples=["CodeNamespace", "UnityHelpers", "CustomTools"]
        )] = None,
        includes: Annotated[Optional[str], Field(
            title="引用的命名空间",
            description="引用的using语句列表，Json数组格式。默认包含: System, System.Collections, System.Collections.Generic, System.Linq, System.Text, System.IO, UnityEngine, UnityEditor, System.Reflection, UnityEngine.SceneManagement, UnityEditor.SceneManagement, UnityEngine.AI, UnityEngine.Rendering, UnityEngine.UI, UnityEngine.EventSystems",
            default=None,
            examples=[
                '["UnityEngine.UI", "TMPro"]',
                '["System.Threading.Tasks", "UnityEngine.Networking"]'
            ]
        )] = None,
        parameters: Annotated[Optional[str], Field(
            title="方法参数",
            description="方法参数，Json数组格式",
            default=None,
            examples=['[1, "test", true]', '["param1", 42]']
        )] = None,
        timeout: Annotated[Optional[int], Field(
            title="超时时间",
            description="代码编译和执行的超时时间（秒），默认30秒",
            default=30,
            ge=1,
            le=120
        )] = 30,
        cleanup: Annotated[bool, Field(
            title="自动清理",
            description="执行完成后是否清理临时文件，默认true",
            default=True
        )] = True,
        return_output: Annotated[bool, Field(
            title="返回输出",
            description="是否捕获并返回控制台输出，默认true",
            default=True
        )] = True
    ) -> Dict[str, Any]:
        """Unity C#代码运行工具，支持编译执行C#代码和语法验证。

        提供完整的Unity API访问权限，适用于：
        - 快速原型：测试Unity API调用
        - 脚本验证：验证C#代码语法正确性
        - 自动化操作：执行复杂的Unity对象操作
        - 调试工具：运行调试和分析代码
        
        默认引用的命名空间：
        - System, System.Collections, System.Collections.Generic
        - System.Linq, System.Text, System.IO, System.Reflection
        - UnityEngine, UnityEditor
        - UnityEngine.SceneManagement, UnityEditor.SceneManagement
        - UnityEngine.AI, UnityEngine.Rendering
        - UnityEngine.UI, UnityEngine.EventSystems
        
        支持两种代码格式：
        1. 顶层语句（自动包装为方法）：直接写代码，如 "Debug.Log(\"Hello\");"
        2. 完整类定义：包含using、namespace、class的完整代码
        """
        
        return send_to_unity("code_runner", {
            "action": action,
            "code": code,
            "description": description,
            "class_name": class_name,
            "entry_method": entry_method,
            "namespace": namespace,
            "includes": includes,
            "parameters": parameters,
            "timeout": timeout,
            "cleanup": cleanup,
            "return_output": return_output
        })
