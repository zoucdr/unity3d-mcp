"""
Shader管理工具
管理Unity中的Shader资源，包括创建、修改、删除和获取信息

支持的功能：
- 创建Shader：创建新的Shader文件，可自定义代码或使用模板
- 修改Shader：更新Shader代码和属性
- 复制Shader：复制现有Shader创建新变体
- 编译验证：编译Shader并检查错误
- 搜索管理：搜索、移动、重命名Shader文件
"""

from typing import Dict, Any, Optional, List
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity

def register_edit_shader_tools(mcp: FastMCP):
    @mcp.tool("edit_shader")
    def edit_shader(
        ctx: Context,
        action: str = Field(
            ...,
            title="操作类型",
            description="操作类型：create(创建), modify(修改), delete(删除), get_info(获取信息), search(搜索), duplicate(复制), move(移动), rename(重命名), compile(编译), validate(验证)",
            examples=["create", "modify", "delete", "get_info", "search", "duplicate", "move", "rename", "compile", "validate"]
        ),
        path: str = Field(
            ...,
            title="Shader路径",
            description="Shader路径，Unity标准格式：Assets/Shaders/MyShader.shader",
            examples=["Assets/Shaders/MyShader.shader", "Assets/Materials/Shaders/CustomShader.shader"]
        ),
        shader_name: Optional[str] = Field(
            None,
            title="Shader名称",
            description="Shader名称",
            examples=["Custom/MyShader", "Unlit/MyShader", "Standard/MyShader"]
        ),
        shader_type: Optional[str] = Field(
            None,
            title="Shader类型",
            description="Shader类型",
            examples=["Unlit", "Standard", "Custom", "UI", "Sprite", "Particle"]
        ),
        shader_code: Optional[str] = Field(
            None,
            title="Shader代码",
            description="Shader代码内容"
        ),
        properties: Optional[Dict[str, Any]] = Field(
            None,
            title="属性字典",
            description="属性字典，用于设置Shader的属性",
            examples=[{"_MainTex": "white", "_Color": [1, 0, 0, 1], "_Metallic": 0.5}]
        ),
        destination: Optional[str] = Field(
            None,
            title="目标路径",
            description="目标路径（移动/复制时使用）",
            examples=["Assets/Shaders/MyShaderCopy.shader", "Assets/Backup/CustomShader.shader"]
        ),
        query: Optional[str] = Field(
            None,
            title="搜索模式",
            description="搜索模式，如*.shader",
            examples=["*.shader", "Custom*", "Unlit*"]
        ),
        recursive: Optional[bool] = Field(
            True,
            title="递归搜索",
            description="是否递归搜索子文件夹"
        ),
        force: Optional[bool] = Field(
            False,
            title="强制执行",
            description="是否强制执行操作（覆盖现有文件等）"
        ),
        create_folder: Optional[bool] = Field(
            True,
            title="创建文件夹",
            description="是否自动创建不存在的文件夹"
        ),
        backup: Optional[bool] = Field(
            True,
            title="备份",
            description="是否在修改前备份原文件"
        ),
        validate_syntax: Optional[bool] = Field(
            True,
            title="验证语法",
            description="是否验证Shader语法"
        ),
        compile_shader: Optional[bool] = Field(
            True,
            title="编译Shader",
            description="是否编译Shader"
        ),
        check_errors: Optional[bool] = Field(
            True,
            title="检查错误",
            description="是否检查编译错误"
        ),
        apply_immediately: Optional[bool] = Field(
            True,
            title="立即应用",
            description="是否立即应用更改"
        ),
        mark_dirty: Optional[bool] = Field(
            True,
            title="标记为脏",
            description="是否标记资源为已修改"
        ),
        save_assets: Optional[bool] = Field(
            True,
            title="保存资源",
            description="是否保存资源到磁盘"
        ),
        refresh_assets: Optional[bool] = Field(
            True,
            title="刷新资源",
            description="是否刷新资源数据库"
        ),
        include_variants: Optional[bool] = Field(
            False,
            title="包含变体",
            description="是否包含Shader变体"
        ),
        platform_specific: Optional[bool] = Field(
            False,
            title="平台特定",
            description="是否生成平台特定的Shader"
        ),
        optimization_level: Optional[str] = Field(
            None,
            title="优化级别",
            description="优化级别",
            examples=["None", "Low", "Medium", "High"]
        ),
        debug_mode: Optional[bool] = Field(
            False,
            title="调试模式",
            description="是否启用调试模式"
        )
    ) -> Dict[str, Any]:
        """
        Shader管理工具，用于管理Unity中的Shader资源。
        
        支持的操作:
        - create: 创建新的Shader文件
        - modify: 修改Shader代码和属性
        - delete: 删除Shader文件
        - get_info: 获取Shader信息
        - search: 搜索Shader文件
        - duplicate: 复制Shader创建新变体
        - move/rename: 移动或重命名Shader文件
        - compile: 编译Shader
        - validate: 验证Shader语法
        
        示例用法：
        1. 创建新的Shader:
           {"action": "create", "path": "Assets/Shaders/MyShader.shader", "shader_name": "Custom/MyShader", "shader_code": "Shader \"Custom/MyShader\" {...}"}
           
        2. 修改Shader:
           {"action": "modify", "path": "Assets/Shaders/MyShader.shader", "shader_code": "Shader \"Custom/MyShader\" {...}"}
           
        3. 复制Shader:
           {"action": "duplicate", "path": "Assets/Shaders/MyShader.shader", "destination": "Assets/Shaders/MyShaderCopy.shader"}
           
        4. 编译Shader:
           {"action": "compile", "path": "Assets/Shaders/MyShader.shader"}
           
        5. 搜索Shader:
           {"action": "search", "query": "Custom*", "recursive": true}
        """
        return send_to_unity("edit_shader", {
            "action": action,
            "path": path,
            "shader_name": shader_name,
            "shader_type": shader_type,
            "shader_code": shader_code,
            "properties": properties,
            "destination": destination,
            "query": query,
            "recursive": recursive,
            "force": force,
            "create_folder": create_folder,
            "backup": backup,
            "validate_syntax": validate_syntax,
            "compile_shader": compile_shader,
            "check_errors": check_errors,
            "apply_immediately": apply_immediately,
            "mark_dirty": mark_dirty,
            "save_assets": save_assets,
            "refresh_assets": refresh_assets,
            "include_variants": include_variants,
            "platform_specific": platform_specific,
            "optimization_level": optimization_level,
            "debug_mode": debug_mode
        })