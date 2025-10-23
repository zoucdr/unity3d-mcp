"""
UI制作规则文件管理工具
管理UI制作规则文件，包括获取原型图片、记录修改、批量记录节点重命名和已下载Sprite信息

支持的操作:
- get_prototype_pic: 获取原型图片（Base64格式）
- record_modify: 记录UI修改记录
- record_renames: 批量记录节点重命名信息
- get_renames: 获取节点重命名信息
- record_download_sprites: 批量记录已下载的节点Sprite信息
- get_download_sprites: 获取已下载的节点Sprite信息

注意: 创建规则功能已移至 Window/Mcp/Rules 编辑器窗口
"""

from typing import Annotated, Dict, Any, Optional
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from .call_up import send_to_unity


def register_ui_rule_manage_tools(mcp: FastMCP):
    @mcp.tool("ui_rule_manage")
    def ui_rule_manage(
        ctx: Context,
        action: Annotated[str, Field(
            title="操作类型",
            description="操作类型: get_prototype_pic(获取原型图片base64), record_modify(记录修改), record_renames(批量记录节点重命名信息), get_renames(获取节点重命名信息), record_download_sprites(批量记录已下载sprite信息), get_download_sprites(获取已下载sprite信息)",
            examples=["get_prototype_pic", "record_modify", "record_renames", "get_renames", "record_download_sprites", "get_download_sprites"]
        )],
        name: Annotated[str, Field(
            title="UI名称",
            description="UI名称，用于查找和记录",
            examples=["SimpleUI", "MainMenuUI", "BattleUI"]
        )],
        modify_desc: Annotated[Optional[str], Field(
            title="修改描述",
            description="修改描述，用于record_modify操作",
            default=None,
            examples=["调整按钮位置", "更新图片资源", "修改文本内容"]
        )] = None,
        save_path: Annotated[Optional[str], Field(
            title="保存路径",
            description="保存路径（保留用于向后兼容，实际创建规则请使用 Window/Mcp/Rules 窗口）",
            default=None,
            examples=["Assets/ScriptableObjects", "Assets/UI/Rules"]
        )] = None,
        properties: Annotated[Optional[str], Field(
            title="属性数据",
            description="属性数据，Json格式字符串（保留用于向后兼容，实际创建规则请使用 Window/Mcp/Rules 窗口）",
            default=None,
            examples=['{"link_url":"https://figma.com/...", "picture_url":"Assets/Pics/SimpleUI", "image_scale":1}']
        )] = None,
        names_data: Annotated[Optional[Dict[str, Any]], Field(
            title="节点重命名数据",
            description="节点重命名数据，字典格式。支持两种格式：1) 详细格式 {\"node_id\":{\"name\":\"new_name\",\"originName\":\"orig_name\"}} 2) 简单格式 {\"node_id\":\"node_name\"}，用于record_renames操作",
            default=None,
            examples=[{"1:4":{"name":"Image1","originName":"image 1"}}, {"1:5":{"name":"TitleText","originName":"title text"}}]
        )] = None,
        sprites_data: Annotated[Optional[Dict[str, str]], Field(
            title="已下载Sprite数据",
            description="已下载Sprite数据，字典格式 {\"node_id\":\"file_name\"}，用于record_download_sprites操作",
            default=None,
            examples=[{"1:4":"image1.png","1:5":"image2.png"}, {"1:6":"Assets/Pics/SimpleUI/background.png"}]
        )] = None,
        auto_load_sprites: Annotated[Optional[bool], Field(
            title="自动加载Sprite",
            description="是否自动从Assets文件夹加载sprite（基于fileName），默认为true",
            default=True
        )] = True
    ) -> Dict[str, Any]:
        """
        UI制作规则文件管理工具
        
        主要功能：
        1. get_prototype_pic - 获取原型图片的Base64编码
        2. record_modify - 记录UI修改记录（带时间戳）
        3. record_renames - 批量记录Figma节点ID到Unity对象名的重命名映射
        4. get_renames - 获取已记录的节点重命名信息
        5. record_download_sprites - 批量记录已下载的节点Sprite信息（自动加载sprite）
        6. get_download_sprites - 获取已记录的已下载Sprite信息
        
        示例用法：
        1. 获取原型图片:
           {"action": "get_prototype_pic", "name": "SimpleUI"}
        
        2. 记录修改:
           {"action": "record_modify", "name": "SimpleUI", "modify_desc": "调整按钮位置"}
        
        3. 记录节点重命名:
           {"action": "record_renames", "name": "SimpleUI", "names_data": {"1:2":{"name":"RootFrame","originName":"RootFrame"},"1:3":{"name":"TitleText","originName":"TitleText"},"1:4":{"name":"Image1","originName":"image 1"}}}

        4. 记录已下载的Sprite信息:
           {"action": "record_download_sprites", "name": "SimpleUI", "sprites_data": {"1:4":"image1_xxxxxx.png","1:5":"image2_xxxxxx.png","1:6":"image3_xxxxxx.png"}}
        """
        return send_to_unity("ui_rule_manage", {
            "action": action,
            "name": name,
            "modify_desc": modify_desc,
            "save_path": save_path,
            "properties": properties,
            "names_data": names_data,
            "sprites_data": sprites_data,
            "auto_load_sprites": auto_load_sprites
        })
