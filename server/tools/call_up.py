"""
Unity MCP 核心调用工具，包含单个和批量Unity函数调用功能。

⚠️ 重要说明：
- 所有MCP函数调用（除async_call和batch_call外）都必须通过此文件中的函数调用
- 这是Unity MCP系统的核心调用入口，所有其他工具函数都通过这里转发到Unity
"""

import json
from typing import Annotated, List, Dict, Any, Literal
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context,Image
from connection import get_unity_connection

# 创建资源对象的辅助函数
def create_resource_object(resource_dict):
    """根据资源类型创建对应的资源对象（Image或Audio），如果不是资源类型则返回原始数据"""
    if not isinstance(resource_dict, dict):
        return resource_dict
    
    resource_type = resource_dict.get("type")
    
    if resource_type == "image":
        return Image(
            path=resource_dict.get("path", "").replace("\\", "/"),
            format=resource_dict.get("format", "png")
        )
    # elif resource_type == "audio":
    #     return Audio(
    #         path=resource_dict.get("path", "").replace("\\", "/"),
    #         format=resource_dict.get("format", "wav")
    #     )
    else:
        # 不是资源类型，返回原始数据
        return resource_dict

# 公共资源处理函数
def process_resources(resources):
    """处理resources中的资源，转换为Image或Audio对象，支持普通数据和资源的混合列表"""
    # 如果resources直接是一个资源对象
    if isinstance(resources, dict) and "type" in resources:
        return create_resource_object(resources)
    
    # 如果resources是数组（多个资源，可能包含普通数据和图片/音频资源）
    if isinstance(resources, list):
        return [create_resource_object(resource) for resource in resources]
    
    # 如果resources是字典的字典（多个资源）
    if isinstance(resources, dict):
        return [create_resource_object(value) for value in resources.values()]
    
    # 如果不匹配任何格式，直接返回原始resources
    return resources

# 资源类型处理
def decord_result(result):
    """解码Unity返回的结果，处理Image和Audio资源"""
    # 处理单个结果中的resources
    if isinstance(result, dict) and "resources" in result:
        processed_resources = process_resources(result["resources"])
        del result["resources"]
        final_result = []
        final_result.append(result)
        if  isinstance(processed_resources, list):
            final_result.extend(processed_resources)
        else:
            final_result.append(processed_resources)
            pass
        return final_result
    return result
# 批量处理结果
def decord_batch_result(result):
    """处理批量结果中的resources"""
    # 处理批处理结果中的resources（results数组）
    if isinstance(result, dict) and "results" in result and isinstance(result["results"], list):
        new_result = []
        for sub_result in result["results"]:
            if isinstance(sub_result, dict) and "resources" in sub_result:
                # 处理resources
                processed_resources = process_resources(sub_result["resources"])
                # 删除resources字段
                del sub_result["resources"]
                # 然后将处理后的资源附加到列表
                if isinstance(processed_resources, list):
                    new_result.extend(processed_resources)
                else:
                    new_result.append(processed_resources)

        # 收集到资源,返回新格式
        if len(new_result) > 0:
            new_result.insert(0, result)
            return new_result
    
    # 如果没有匹配到资源，直接返回原始结果
    return result

# 发送命令到Unity
def send_to_unity(func,args):
        try:
            # 验证函数名称
            if not func or not isinstance(func, str):
                return {
                    "success": False,
                    "error": "函数名称无效或为空",
                    "result": None
                }
            
            # 验证参数类型
            if not isinstance(args, dict):
                return {
                    "success": False,
                    "error": "参数必须是对象类型",
                    "result": None
                }
            
            # 获取Unity连接实例
            bridge = get_unity_connection()
            
            if bridge is None:
                return {
                    "success": False,
                    "error": "无法获取Unity连接",
                    "result": None
                }
            
            # 只传递args中不为空的参数
            params = {k: v for k, v in args.items() if v is not None}

            # 使用带重试机制的命令发送
            result = bridge.send_command_with_retry(func, params, max_retries=2)
            
            # 确保返回结果包含success标志
            if isinstance(result, dict):
                return decord_result(result)
            else:
                return {
                    "success": True,
                    "result": result,
                    "error": None
                }
                
        except json.JSONDecodeError as e:
            return {
                "success": False,
                "error": f"参数序列化失败: {str(e)}",
                "result": None
            }
        except ConnectionError as e:
            return {
                "success": False,
                "error": f"Unity连接错误: {str(e)}",
                "result": None
            }
        except Exception as e:
            return {
                "success": False,
                "error": f"函数调用失败: {str(e)}",
                "result": None
            }

def register_call_tools(mcp: FastMCP):
    @mcp.tool("async_call")
    def async_call(
        ctx: Context,
        id: Annotated[str, Field(
            title="任务ID",
            description="异步调用的唯一标识符"
        )],
        type: Annotated[Literal['in', 'out'], Field(
            title="操作类型",
            description="in: 执行调用, out: 获取调用结果"
        )],
        func: Annotated[str, Field(
            title="Unity函数名称",
            default=None,
            description="要调用的Unity函数名称。当type为'in'时必需。",
            examples=["hierarchy_create", "edit_gameobject", "base_editor", "gameplay", "console_write"]
        )] = None,
        args: Annotated[Dict[str, Any], Field(
            title="函数参数",
            default=None,
            description="传递给Unity函数的参数字典。当type为'in'时使用。",
            examples=[
                {"source": "primitive", "name": "Cube", "primitive_type": "Cube"},
                {"path": "Player", "action": "add_component", "component_type": "Rigidbody"},
                {"action": "play"}
            ]
        )] = {}
    ):
        """异步函数调用工具，用于调用Unity MCP函数并获取结果。（基础工具）
        
        - type='in': 发起一个异步调用。需要提供 id, func, 和 args。
        - type='out': 获取一个异步调用的结果。需要提供 id。
        
        支持的函数包括但不限于：
        - hierarchy_create: 创建GameObject
        - edit_gameobject: 编辑GameObject
        - base_editor: 编辑器管理
        - gameplay: 游戏玩法控制
        - console_write: 控制台输出
        - 以及其他所有MCP工具函数
        """
        
        try:
            if type == 'in':
                if not func or not isinstance(func, str):
                    return {
                        "success": False,
                        "error": "当type为'in'时, 'func'是必需的",
                        "result": None
                    }
                if not isinstance(args, dict):
                    return {
                        "success": False,
                        "error": "参数'args'必须是对象类型",
                        "result": None
                    }
            elif type == 'out':
                # 'func' and 'args' can be None
                pass
            else:
                return {
                    "success": False,
                    "error": f"无效的'type'值: {type}",
                    "result": None
                }

            # 获取Unity连接实例
            bridge = get_unity_connection()
            
            if bridge is None:
                return {
                    "success": False,
                    "error": "无法获取Unity连接",
                    "result": None
                }
            
            # 准备发送给Unity的参数
            params = {
                "id": id,
                "type": type,
                "func": func,
                "args": args
            }
            # 只传递params中不为空的参数
            params = {k: v for k, v in params.items() if v is not None}
            # 使用带重试机制的命令发送
            result = bridge.send_command_with_retry("async_call", params, max_retries=2)
            
            # 确保返回结果包含success标志
            if isinstance(result, dict):
                return decord_result(result)
            else:
                return {
                    "success": True,
                    "result": result,
                    "error": None
                }
                
        except json.JSONDecodeError as e:
            return {
                "success": False,
                "error": f"参数序列化失败: {str(e)}",
                "result": None
            }
        except ConnectionError as e:
            return {
                "success": False,
                "error": f"Unity连接错误: {str(e)}",
                "result": None
            }
        except Exception as e:
            return {
                "success": False,
                "error": f"函数调用失败: {str(e)}",
                "result": None
            }

    @mcp.tool("batch_call")
    def batch_call(
        ctx: Context,
        funcs: Annotated[List[Dict[str, Any]], Field(
            title="函数调用列表",
            description="要批量执行的Unity函数调用列表，按顺序执行。⚠️ 重要：每个元素必须包含func和args字段，func指定要调用的MCP函数名，args传递该函数的所有参数",
            min_length=1,
            max_length=50,
            examples=[
                [
                    {"func": "hierarchy_create", "args": {"source": "primitive", "primitive_type": "Cube", "name": "Enemy"}},
                    {"func": "edit_gameobject", "args": {"path": "Enemy", "action": "add_component", "component_type": "Rigidbody"}}
                ],
                [
                    {"func": "base_editor", "args": {"action": "play"}},
                    {"func": "gameplay", "args": {"action": "screenshot", "format": "PNG"}},
                    {"func": "base_editor", "args": {"action": "stop"}}
                ]
            ]
        )]
    ):
        """批量函数调用工具，可以按顺序调用Unity中的多个MCP函数并收集所有返回值。（基础工具）
        
        - 每个函数调用元素必须包含func（函数名）和args（参数字典）字段
        - 函数名必须是有效的MCP函数名，如hierarchy_create、edit_gameobject等
        - 参数格式必须严格按照目标函数的定义

        支持事务性操作和批量处理，常用场景：
        - 创建并配置GameObject：创建 → 设置属性 → 添加组件
        - 场景管理：播放 → 截图 → 停止
        - 批量创建：创建多个不同类型的GameObject
        - UI创建：创建Canvas → 创建UI元素 → 设置布局
        
        # 批量调用示例：
        
         示例1：批量创建Cube和添加Rigidbody组件
         [
             {"func": "hierarchy_create", "args": {"source": "primitive", "primitive_type": "Cube", "name": "Enemy"}},
             {"func": "edit_gameobject", "args": {"path": "Enemy", "action": "add_component", "component_type": "Rigidbody"}}
         ]
        
         示例2：先播放，执行截图，再停止
         [
             {"func": "base_editor", "args": {"action": "play"}},
             {"func": "gameplay", "args": {"action": "screenshot", "format": "PNG"}},
             {"func": "base_editor", "args": {"action": "stop"}}
         ]
        
         示例3：批量生成多个UI按钮
         [
             {"func": "hierarchy_create", "args": {"source": "ui", "ui_type": "Button", "name": "Button1"}},
             {"func": "hierarchy_create", "args": {"source": "ui", "ui_type": "Button", "name": "Button2"}},
             {"func": "hierarchy_create", "args": {"source": "ui", "ui_type": "Button", "name": "Button3"}}
         ]
        """
        
        # 获取Unity连接实例
        bridge = get_unity_connection()
        
        try:
            # 验证输入参数
            if not isinstance(funcs, list):
                return {
                    "success": False,
                    "results": [],
                    "errors": ["funcs参数必须是数组类型"],
                    "total_calls": 0,
                    "successful_calls": 0,
                    "failed_calls": 1
                }
            
            # 基本的函数调用格式验证
            for i, func_call in enumerate(funcs):
                if not isinstance(func_call, dict):
                    return {
                        "success": False,
                        "results": [],
                        "errors": [f"第{i+1}个函数调用必须是对象类型"],
                        "total_calls": len(funcs),
                        "successful_calls": 0,
                        "failed_calls": 1
                    }
                
                if "func" not in func_call or not isinstance(func_call.get("func"), str):
                    return {
                        "success": False,
                        "results": [],
                        "errors": [f"第{i+1}个函数调用的func字段无效或为空"],
                        "total_calls": len(funcs),
                        "successful_calls": 0,
                        "failed_calls": 1
                    }
                
                if "args" not in func_call or not isinstance(func_call.get("args"), dict):
                    return {
                        "success": False,
                        "results": [],
                        "errors": [f"第{i+1}个函数调用的args字段必须是对象类型"],
                        "total_calls": len(funcs),
                        "successful_calls": 0,
                        "failed_calls": 1
                    }
            
            # 使用带重试机制的命令发送到Unity的functions_call处理器
            result = bridge.send_command_with_retry("batch_call", funcs, max_retries=1)
            
            # Unity的functions_call处理器返回的结果已经是完整的格式，直接返回data部分
            if isinstance(result, dict) and "data" in result:
                return decord_batch_result(result["data"])
            else:
                # 如果返回格式不符合预期，包装成标准格式
                return {
                    "success": True,
                    "results": [result],
                    "errors": [None],
                    "total_calls": len(funcs),
                    "successful_calls": 1,
                    "failed_calls": 0
                }
            
        except Exception as e:
            return {
                "success": False,
                "results": [],
                "errors": [f"批量调用转发失败: {str(e)}"],
                "total_calls": len(funcs) if isinstance(funcs, list) else 0,
                "successful_calls": 0,
                "failed_calls": 1
            }
