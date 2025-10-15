"""
UnityCode execution tool，IncludePythonCode execution andC#Code compile-and-run capability。
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import get_common_call_response


def register_run_code_tools(mcp: FastMCP):
    @mcp.tool("python_runner")
    def python_runner(
        ctx: Context,
        action: Annotated[str, Field(
            title="PythonOperation type",
            description="To executePythonOperation: execute(Execute code), validate(Validate code), install_package(Install package)",
            examples=["execute", "validate", "install_package"]
        )],
        code: Annotated[Optional[str], Field(
            title="PythonCode",
            description="To execute or validatePythonCode，SupportUTF-8Encoding",
            default=None,
            examples=[
                "print('Hello Unity!')",
                "import numpy as np\nprint(np.array([1,2,3]))",
                "# Create3DModel\nimport trimesh\nmesh = trimesh.creation.box()"
            ]
        )] = None,
        package_name: Annotated[Optional[str], Field(
            title="Package name",
            description="To installPythonPackage name，Only wheninstall_packageUsed during operation",
            default=None,
            examples=["numpy", "trimesh", "matplotlib", "opencv-python"]
        )] = None,
        version: Annotated[Optional[str], Field(
            title="Package version",
            description="Package version to install，Leave empty to install latest",
            default=None,
            examples=["1.21.0", ">=1.0.0", "~=2.1.0"]
        )] = None,
        timeout: Annotated[Optional[int], Field(
            title="Timeout",
            description="Timeout for code execution or package install（Seconds）",
            default=30,
            ge=1,
            le=300
        )] = 30,
        cleanup: Annotated[bool, Field(
            title="Auto clean",
            description="Whether to auto-clean temp files and variables after execution",
            default=True
        )] = True
    ) -> Dict[str, Any]:
        """Unity PythonCode execution tool，Supports executionPythonCode、Validate syntax and install packages。

        Provide completePythonRuntime environment，Suitable for：
        - Data processing：UseNumPy、PandasUse libraries to process data
        - 3DModeling：UseTrimesh、Open3DUse libraries to build models
        - Image processing：UseOpenCV、PILUse libraries for image processing
        - Machine learning：RunTensorFlow、PyTorchAnd models

        
        """
        
        # ⚠️ Important notes：This function only provides parameter docs
        # Use in actual calls single_call Function
        # Example：single_call(func="python_runner", args={"action": "execute", "code": "print('Hello')"})
        
        return get_common_call_response("python_runner")


    @mcp.tool("code_runner")
    def code_runner(
        ctx: Context,
        action: Annotated[str, Field(
            title="C#Operation type",
            description="To executeC#Operation: execute(Compile and run), validate(Validate syntax)",
            examples=["execute", "validate"]
        )],
        code: Annotated[str, Field(
            title="C#Code",
            description="To compileC#Code，Fully supportedUnity APIAccess。Supports top-level statements and full class definitions",
            examples=[
                "Debug.Log(\"Hello from C#!\");",
                "var go = new GameObject(\"TestObject\"); go.transform.position = Vector3.zero;",
                "var terrain = GameObject.Find(\"Terrain\").GetComponent<Terrain>();\nterrain.terrainData.SetHeights(0, 0, new float[513, 513]);"
            ]
        )],
        class_name: Annotated[Optional[str], Field(
            title="Class name",
            description="Class name，Defaults toCodeClass",
            default=None,
            examples=["CodeClass", "TerrainHelper", "MeshGenerator"]
        )] = None,
        entry_method: Annotated[Optional[str], Field(
            title="Entry method",
            description="Entry method name，Defaults toRun",
            default=None,
            examples=["Run", "Execute", "Main"]
        )] = None,
        namespace: Annotated[Optional[str], Field(
            title="Namespace",
            description="Namespace，Defaults toCodeNamespace",
            default=None,
            examples=["CodeNamespace", "UnityHelpers", "CustomTools"]
        )] = None,
        includes: Annotated[Optional[str], Field(
            title="Referenced namespaces",
            description="ReferencedusingStatement list，JsonArray format。Included by default: System, System.Collections, System.Collections.Generic, System.Linq, System.Text, System.IO, UnityEngine, UnityEditor, System.Reflection, UnityEngine.SceneManagement, UnityEditor.SceneManagement, UnityEngine.AI, UnityEngine.Rendering, UnityEngine.UI, UnityEngine.EventSystems",
            default=None,
            examples=[
                '["UnityEngine.UI", "TMPro"]',
                '["System.Threading.Tasks", "UnityEngine.Networking"]'
            ]
        )] = None,
        parameters: Annotated[Optional[str], Field(
            title="Method parameters",
            description="Method parameters，JsonArray format",
            default=None,
            examples=['[1, "test", true]', '["param1", 42]']
        )] = None,
        timeout: Annotated[Optional[int], Field(
            title="Timeout",
            description="Timeout for compile and run（Seconds），Default30Seconds",
            default=30,
            ge=1,
            le=120
        )] = 30,
        cleanup: Annotated[bool, Field(
            title="Auto clean",
            description="Whether to clean temp files after execution，Defaulttrue",
            default=True
        )] = True,
        return_output: Annotated[bool, Field(
            title="Return output",
            description="Whether to capture and return console output，Defaulttrue",
            default=True
        )] = True
    ) -> Dict[str, Any]:
        """Unity C#Code execution tool，Supports compile & runC#Code and syntax validation。（Secondary tool）

        Provide completeUnity APIAccess permissions，Suitable for：
        - Rapid prototyping：TestUnity APICall
        - Script validation：ValidateC#Code syntax correctness
        - Automation operations：Execute complexUnityObject operations
        - Debugging tools：Run
        
        Default imported namespaces：
        - System, System.Collections, System.Collections.Generic
        - System.Linq, System.Text, System.IO, System.Reflection
        - UnityEngine, UnityEditor
        - UnityEngine.SceneManagement, UnityEditor.SceneManagement
        - UnityEngine.AI, UnityEngine.Rendering
        - UnityEngine.UI, UnityEngine.EventSystems
        
        Supports two code formats：
        1. Top-level statements（Auto-wrap as a method）：Write code directly，Such as "Debug.Log(\"Hello\");"
        2. Full class definition：Includeusing、namespace、classComplete code
        """
        
        return get_common_call_response("code_runner")
