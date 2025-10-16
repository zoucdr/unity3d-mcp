"""
Unity项目资源创建工具，用于在Assets文件夹中创建各种资源。

支持的功能：
- 从菜单创建资源：menu
- 创建空文件：empty
- 从模板创建资源：template
- 复制现有资源：copy
"""
from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_project_create_tools(mcp: FastMCP):
    @mcp.tool("project_create")
    def project_create(
        ctx: Context,
        source: Annotated[str, Field(
            title="操作类型",
            description="操作类型：menu(菜单), empty(空文件), template(模板), copy(复制)",
            examples=["menu", "empty", "template", "copy"]
        )],
        name: Annotated[str, Field(
            title="资源文件名称",
            description="要创建的资源文件名称",
            examples=["NewMaterial", "PlayerController", "ReadMe"]
        )],
        folder_path: Annotated[Optional[str], Field(
            title="目标文件夹路径",
            description="目标文件夹路径（相对于Assets）",
            default="Assets",
            examples=["Assets/Materials", "Scripts/Player", "Textures"]
        )] = "Assets",
        menu_path: Annotated[Optional[str], Field(
            title="菜单路径",
            description="菜单路径，用于menu类型",
            default=None,
            examples=["Assets/Create/Material", "Assets/Create/C# Script", "Assets/Create/Shader"]
        )] = None,
        template_path: Annotated[Optional[str], Field(
            title="模板文件路径",
            description="模板文件路径，用于template类型",
            default=None,
            examples=["Assets/Templates/Script.cs", "Assets/Templates/Config.json"]
        )] = None,
        copy_source: Annotated[Optional[str], Field(
            title="要复制的资源路径",
            description="要复制的资源路径，用于copy类型",
            default=None,
            examples=["Assets/Materials/Default.mat", "Assets/Prefabs/Player.prefab"]
        )] = None,
        extension: Annotated[Optional[str], Field(
            title="文件扩展名",
            description="文件扩展名（不含.），用于empty类型",
            default="txt",
            examples=["txt", "json", "xml", "md", "cs"]
        )] = "txt",
        content: Annotated[Optional[str], Field(
            title="文件内容",
            description="文件内容（用于empty类型）",
            default="",
            examples=["这是一个示例文档", "# 标题\n\n这是一个Markdown文件"]
        )] = "",
        force: Annotated[Optional[bool], Field(
            title="强制覆盖",
            description="是否强制覆盖已存在的文件",
            default=False
        )] = False,
        open_after_create: Annotated[Optional[bool], Field(
            title="创建后打开",
            description="创建后是否打开文件",
            default=False
        )] = False,
        select_after_create: Annotated[Optional[bool], Field(
            title="创建后选中",
            description="创建后是否选中文件",
            default=True
        )] = True
    ) -> Dict[str, Any]:
        """Unity项目资源创建工具，用于在Assets文件夹中创建各种资源。

        支持多种创建方式，适用于：
        - 从菜单创建资源：通过Unity菜单系统创建资源
        - 创建空文件：创建指定扩展名的空文件
        - 从模板创建资源：使用现有文件作为模板创建新资源
        - 复制现有资源：复制项目中的现有资源

        示例用法：
        1. 从菜单创建材质：
           {"source": "menu", "menu_path": "Assets/Create/Material", "folder_path": "Assets/Materials", "name": "NewMaterial"}

        2. 创建空文本文件：
           {"source": "empty", "name": "ReadMe", "extension": "txt", "folder_path": "Assets/Docs", "content": "这是一个示例文档"}

        3. 从模板创建脚本：
           {"source": "template", "name": "PlayerController", "template_path": "Assets/Scripts/Templates/Controller.cs", "folder_path": "Assets/Scripts/Player"}

        4. 复制现有材质：
           {"source": "copy", "copy_source": "Assets/Materials/DefaultMaterial.mat", "name": "PlayerMaterial", "folder_path": "Assets/Materials/Player"}
        """
        return send_to_unity("project_create", {
            "source": source,
            "name": name,
            "folder_path": folder_path,
            "menu_path": menu_path,
            "template_path": template_path,
            "copy_source": copy_source,
            "extension": extension,
            "content": content,
            "force": force,
            "open_after_create": open_after_create,
            "select_after_create": select_after_create
        })
