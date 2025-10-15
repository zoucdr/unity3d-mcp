"""
Unity MCP Core invocation utility，include single and batchUnityfunction callFunction。

⚠️ important notes：
- allMCPfunction call（exceptsingle_callandbatch_calloutside）all must be called via functions in this file
- Cannot be called directlyhierarchy_create、edit_gameobject、base_editorand functions
- Must usesingle_callperform a single function call，or usebatch_callPerform batch calls
- this isUnity MCPcore invocation entry of the system，all other tool functions are forwarded here toUnity
"""
import json
from typing import Annotated, List, Dict, Any, Literal
from pydantic import Field
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

# common error message text
def get_common_call_response(func_name: str) -> dict:
    """
    get common error response format
    
    Args:
        func_name: function name
        
    Returns:
        standard error response dict
    """
    return {
        "success": False,
        "error": "please use single_call(func='{func_name}', args={{...}}) To call thisfunction".format(func_name=func_name),
        "data": None
    }

def register_call_tools(mcp: FastMCP):
    @mcp.tool("single_call")
    def single_call(
        ctx: Context,
        func: Annotated[str, Field(
            title="Unityfunction name",
            description="to callUnityfunction name。⚠️ important：allMCPfunction call（exceptsingle_callandbatch_calloutside）must be invoked via this function",
            examples=["hierarchy_create", "edit_gameobject", "base_editor", "gameplay", "console_write"]
        )],
        args: Annotated[Dict[str, Any], Field(
            title="functionParameters",
            description="pass toUnityparameter dict of the function。argument format must strictly follow target function definition，all parameters go through thisargspass as dict",
            default_factory=dict,
            examples=[
                {"source": "primitive", "name": "Cube", "primitive_type": "Cube"},
                {"path": "Player", "action": "add_component", "component_type": "Rigidbody"},
                {"action": "play"}
            ]
        )] = {}
    ) -> Dict[str, Any]:
        """single function call utility，Used to invokeallUnity MCPfunction。（primary tool）
        
        ⚠️ important notes：
        - allMCPfunction call（exceptsingle_callandbatch_calloutside）must be invoked via this function
        - Cannot be called directlyhierarchy_create、edit_gameobjectand functions，must go throughsingle_call
        - funcparameter specifies function name to call，argsparameters pass all arguments required by the function
        
        supported functions include but are not limited to：
        - hierarchy_create: createGameObject
        - edit_gameobject: editGameObject
        - base_editor: editManager
        - gameplay: Gameplay control
        - console_write: Console output
        - And otherallMCPutility function
        """
        
        try:
            # Validatefunction name
            if not func or not isinstance(func, str):
                return {
                    "success": False,
                    "error": "function name invalid or empty",
                    "result": None
                }
            
            # Validate parameter types
            if not isinstance(args, dict):
                return {
                    "success": False,
                    "error": "argument must be an object",
                    "result": None
                }
            
            # getUnityconnection instance
            bridge = get_unity_connection()
            
            if bridge is None:
                return {
                    "success": False,
                    "error": "unable to obtainUnityconnection",
                    "result": None
                }
            
            # Prepare to send toUnityparameters of
            params = {
                "func": func,
                "args": args
            }
            
            # send command with retry mechanism
            result = bridge.send_command_with_retry("single_call", params, max_retries=2)
            
            # ensure result containssuccessflag
            if isinstance(result, dict):
                return result
            else:
                return {
                    "success": True,
                    "result": result,
                    "error": None
                }
                
        except json.JSONDecodeError as e:
            return {
                "success": False,
                "error": f"parameter serialization failed: {str(e)}",
                "result": None
            }
        except ConnectionError as e:
            return {
                "success": False,
                "error": f"Unityconnection error: {str(e)}",
                "result": None
            }
        except Exception as e:
            return {
                "success": False,
                "error": f"function callFailed: {str(e)}",
                "result": None
            }

    @mcp.tool("batch_call")
    def batch_call(
        ctx: Context,
        funcs: Annotated[List[Dict[str, Any]], Field(
            title="function callList",
            description="To execute in batchofUnityfunction callList，Execute in order。⚠️ important：each element must includefuncandargsfield，funcSpecifyto callMCPfunction name，argspass all parameters for the function",
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
    ) -> Dict[str, Any]:
        """batch function call utility，can be called in sequenceUnityInofMultipleMCPinvoke functions and collect all returns。（primary tool）
        
        ⚠️ important notes：
        - allMCPfunction call（exceptsingle_callandbatch_calloutside）must be invoked via this function
        - each function call item must includefunc（function name）andargs（parameter dict）field
        - function name must be validMCPfunction name，such ashierarchy_create、edit_gameobjectetc
        - argument format must strictly follow target function definition

        supports transactional and batch operations，Common use cases：
        - createAnd configureGameObject：create → set property → add component
        - scene management：play → screenshot → stop
        - batch create：create multiple different types ofGameObject
        - UIcreate：createCanvas → createUIelement → set layout
        """
        
        # getUnityconnection instance
        bridge = get_unity_connection()
        
        try:
            # Validate input parameters
            if not isinstance(funcs, list):
                return {
                    "success": False,
                    "results": [],
                    "errors": ["funcsarguments must be an array"],
                    "total_calls": 0,
                    "successful_calls": 0,
                    "failed_calls": 1
                }
            
            # basic function call format validation
            for i, func_call in enumerate(funcs):
                if not isinstance(func_call, dict):
                    return {
                        "success": False,
                        "results": [],
                        "errors": [f"No{i+1}each function call must be an object"],
                        "total_calls": len(funcs),
                        "successful_calls": 0,
                        "failed_calls": 1
                    }
                
                if "func" not in func_call or not isinstance(func_call.get("func"), str):
                    return {
                        "success": False,
                        "results": [],
                        "errors": [f"No{i+1}Itemfunction calloffuncfield invalid or empty"],
                        "total_calls": len(funcs),
                        "successful_calls": 0,
                        "failed_calls": 1
                    }
                
                if "args" not in func_call or not isinstance(func_call.get("args"), dict):
                    return {
                        "success": False,
                        "results": [],
                        "errors": [f"No{i+1}Itemfunction callofargsfield must be an object"],
                        "total_calls": len(funcs),
                        "successful_calls": 0,
                        "failed_calls": 1
                    }
            
            # send with retry mechanism toUnityoffunctions_callhandler
            result = bridge.send_command_with_retry("batch_call", funcs, max_retries=1)
            
            # Unityoffunctions_callhandler returns results in complete format，return directlydatasection
            if isinstance(result, dict) and "data" in result:
                return result["data"]
            else:
                # if return format is unexpected，wrap into standard format
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
                "errors": [f"batch call forwarding failed: {str(e)}"],
                "total_calls": len(funcs) if isinstance(funcs, list) else 0,
                "successful_calls": 0,
                "failed_calls": 1
            }
