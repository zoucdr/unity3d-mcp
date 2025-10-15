using UnityEngine;
using UnityMcp;
using UnityMcp.Models;

public class TestJson : MonoBehaviour
{
    void Start()
    {
        // 测试布尔值序列化
        var successResponse = Response.Success("测试成功", true);
        Debug.Log("Success Response: " + successResponse.ToString());
        
        var errorResponse = Response.Error("测试错误", false);
        Debug.Log("Error Response: " + errorResponse.ToString());
        
        // 测试数值序列化
        var numResponse = new JsonClass();
        numResponse.Add("int_value", 123);
        numResponse.Add("float_value", 45.67f);
        Debug.Log("Number Response: " + numResponse.ToString());
        
        // 测试字符串序列化
        var strResponse = new JsonClass();
        strResponse.Add("message", "Hello World");
        Debug.Log("String Response: " + strResponse.ToString());
    }
}