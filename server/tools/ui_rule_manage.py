"""
UI制作规则文件管理工具
管理UI制作规则文件，包括创建、获取规则、添加修改记录、批量记录节点命名和Sprite信息

支持的操作:
- create_rule: 创建UI制作规则
- get_rule: 获取UI制作规则和方案（包含构建步骤、环境等）
- get_prototype_pic: 获取原型图片（Base64格式）
- add_modify: 添加UI修改记录
- record_names: 批量记录节点命名信息
- get_names: 获取节点命名信息
- record_sprites: 批量记录节点Sprite信息
- get_sprites: 获取节点Sprite信息
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
            description="操作类型: create_rule(创建制作方案), get_rule(获取制作方案), get_prototype_pic(获取原型图片base64), add_modify(添加修改记录), record_names(批量记录节点命名信息), get_names(获取节点命名信息), record_sprites(批量记录sprite信息), get_sprites(获取sprite信息)",
            examples=["create_rule", "get_rule", "get_prototype_pic", "add_modify", "record_names", "get_names", "record_sprites", "get_sprites"]
        )],
        name: Annotated[str, Field(
            title="UI名称",
            description="UI名称，用于查找和记录",
            examples=["SimpleUI", "MainMenuUI", "BattleUI"]
        )],
        modify_desc: Annotated[Optional[str], Field(
            title="修改描述",
            description="修改描述，用于add_modify操作",
            default=None,
            examples=["调整按钮位置", "更新图片资源", "修改文本内容"]
        )] = None,
        save_path: Annotated[Optional[str], Field(
            title="保存路径",
            description="保存路径，用于创建新的UIDefineRuleObject，默认为Assets/ScriptableObjects",
            default=None,
            examples=["Assets/ScriptableObjects", "Assets/UI/Rules"]
        )] = None,
        properties: Annotated[Optional[str], Field(
            title="属性数据",
            description="属性数据，Json格式字符串，用于create_rule操作。支持字段：link_url, picture_url, prototype_pic, image_scale, descriptions",
            default=None,
            examples=['{"link_url":"https://figma.com/...", "picture_url":"Assets/Pics/SimpleUI", "image_scale":1}']
        )] = None,
        names_data: Annotated[Optional[str], Field(
            title="节点命名数据",
            description="节点命名数据，Json格式。支持两种格式：1) 详细格式 {\"node_id\":{\"name\":\"new_name\",\"originName\":\"orig_name\"}} 2) 简单格式 {\"node_id\":\"node_name\"}，用于record_names操作",
            default=None,
            examples=['{"1:2":"RootFrame","1:3":"TitleText"}', '{"1:4":{"name":"Image1","originName":"image 1"}}']
        )] = None,
        sprites_data: Annotated[Optional[str], Field(
            title="Sprite数据",
            description="Sprite数据，Json格式 {\"node_id\":\"file_name\"}，用于record_sprites操作",
            default=None,
            examples=['{"1:4":"image1.png","1:5":"image2.png"}', '{"1:6":"Assets/Pics/SimpleUI/background.png"}']
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
        1. create_rule - 创建UI制作规则ScriptableObject
        2. get_rule - 获取UI规则（包含构建步骤、环境、优化规则等）
        3. get_prototype_pic - 获取原型图片的Base64编码
        4. add_modify - 添加UI修改记录（带时间戳）
        5. record_names - 批量记录Figma节点ID到Unity对象名的映射
        6. get_names - 获取已记录的节点命名信息
        7. record_sprites - 批量记录节点Sprite信息（自动加载sprite）
        8. get_sprites - 获取已记录的Sprite信息
        
        示例用法：
        1. 创建UI规则:
           {"action": "create_rule", "name": "SimpleUI", "properties": "{\"link_url\":\"https://figma.com/...\"}"}
        
        2. 获取UI规则:
           {"action": "get_rule", "name": "SimpleUI"}
        
        3. 记录节点命名:
           {"action": "record_names", "name": "SimpleUI", "names_data": "{\"1:2\":\"RootFrame\",\"1:3\":\"TitleText\"}"}
        
        4. 记录Sprite信息:
           {"action": "record_sprites", "name": "SimpleUI", "sprites_data": "{\"1:4\":\"image1.png\",\"1:5\":\"image2.png\"}"}
        
        注意：
        - name参数是必需的，用于识别UI规则
        - 所有Json数据需要以字符串格式传递
        - record_sprites会自动加载sprite资源（auto_load_sprites=true）
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
